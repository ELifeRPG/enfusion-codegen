# Enforce Script API Client Generator — Design

Date: 2026-08-16
Status: Approved for planning

## Problem

ELifeRPG hand-writes its Arma Reforger (Enfusion / Enforce Script) REST API
client against the ELIFE.Core backend: DTO structs, per-response callback
classes, and endpoint methods on a singleton `ELIFE_Api` class (see
`src/Scripts/Game/Core/Api/**` in `ELifeRPG/ArmA-Reforger`, branch
`feature_persistence`). This is repetitive and error-prone — e.g. the
existing hand-written code inconsistently uses `RegV` vs `StartArray` for
array fields across sibling DTOs.

A prior in-house tool, [`ELifeRPG/Code-Generator`](https://github.com/ELifeRPG/Code-Generator),
already automates *half* of this: it reads an OpenAPI spec's
`components.schemas` and emits `JsonApiStruct` DTO/enum classes via
RazorLight templates. It never reads `paths` — every REST call method,
every callback class, and the `ELIFE_Api` singleton are still 100%
hand-written today. That's also where the field-registration bug lives.

## Goal

Build a new standalone code generator that covers the *whole* client:
models (schemas) **and** operations (paths → client methods + callbacks),
producing output that matches the existing hand-written code's shape
closely enough to be a drop-in replacement.

## Why not a real Kiota language extension

Kiota's generator core (`microsoft/kiota`, C#/.NET) has no runtime plugin
system. Adding a language means forking the `kiota` repo itself and
registering, in-tree:

- `Writers/{Language}/{Language}Writer.cs` + one `ICodeElementWriter<T>`
  implementation per `CodeElement` kind (7–17 files depending on language
  complexity — Java is the leanest at 7 files, TypeScript the heaviest at 17).
- `Refiners/{Language}Refiner.cs` extending `CommonLanguageRefiner`
  (naming/reserved-word/import correction) plus reserved-name providers.
- `PathSegmenters/{Language}PathSegmenter.cs`.
- Compile-time registration in ~4-5 places (`GenerationLanguage` enum,
  `LanguageWriter.GetLanguageWriter()`, the `ILanguageRefiner.RefineAsync()`
  switch, `PublicAPIExportService`, `appsettings.json`).
- A separate runtime companion library (abstractions: `IParsable`,
  `IParseNode`, `ISerializationWriter`, `IRequestAdapter`,
  `BaseRequestBuilder`, auth providers) in its own repo, same as every
  other Kiota language (`kiota-dotnet`, `kiota-java`, etc.).

Dart is the one community language that went this route successfully
(started as a community fork, later merged upstream into
`microsoft/kiota`/`microsoft/kiota-dart`). Rust is presently going through
the same process as an active, unmerged fork
(`kiota-community/kiota-rust`). Both are substantial, multi-month C#
compiler-engineering efforts with an uncertain upstream-merge timeline —
disproportionate for a client targeting one internal API.

**Decision**: build a standalone generator, architecturally inspired by
Kiota's model→writer separation (and by `Code-Generator`'s proven
OpenAPI-parsing base), but not literally a Kiota fork. This mirrors what
`Code-Generator` already was, just extended to cover operations.

## Architecture

Four-stage pipeline, single C# dotnet CLI tool:

1. **Parse** — `Microsoft.OpenApi.Readers` reads the spec (file or URL),
   *with* reference resolution enabled this time (the old tool disabled
   it and worked around it by reading `.Reference.Id` directly off
   unresolved nodes — fragile for anything beyond top-level component
   refs).
2. **Build model** — walk the resolved `OpenApiDocument` into an
   Enforce-Script-specific in-memory model: `EsClass`, `EsEnum`,
   `EsProperty`, `EsMethod`, `EsCallbackClass`. This is conceptually
   Kiota's CodeDOM idea, purpose-built rather than generic. All naming,
   type-mapping, and dedup decisions happen here, in plain testable C#.
3. **Refine** (lightweight) — reserved-word checks, response-DTO→callback
   dedup (multiple operations returning the same schema share one
   callback class, matching today's `ELIFE_CharacterDtoListResultDtoCallback`
   reuse), prefix application to client/callback names only (not DTOs).
4. **Write** — one writer function per model-node kind emits `.c` text via
   `IndentedTextWriter`. No templating engine, no decision logic inside
   templates (the old tool's `.cshtml` files mixed `EndsWith("EnumDto")`
   checks into the view layer — that goes away).

This replaces `Code-Generator`'s `TemplateModelFactory` +
`RazorLight` + `.cshtml` stack entirely. The OpenAPI-parsing dependency
(`Microsoft.OpenApi`) and the dotnet-tool CLI shape are kept.

## Model generation (schemas → DTOs)

Same output shape as today's hand-written DTOs:

```c
class CharacterDto : JsonApiStruct
{
	string id;
	string firstName;
	string lastName;

	void CharacterDto()
	{
		RegV("id");
		RegV("firstName");
		RegV("lastName");
	}
}
```

Fixes over `Code-Generator`:
- Real `$ref` resolution (works for nested/inline schemas, not just
  top-level components).
- Arrays of primitives supported (old tool assumed `Items.Reference.Id`
  always exists).
- `nullable` is read from the schema (old tool ignored it entirely).
- Enum member naming does not hard-require the NSwag/Swashbuckle
  `x-enumNames` vendor extension; falls back to a generated name when
  absent (e.g. `Value0`, `Value1`) so the tool isn't coupled to one
  specific backend toolchain.

**Open risk — array field registration.** Hand-written code is
inconsistent: `CharacterDtoListResultDto` uses `RegV("messages")` for its
`array<ref MessageDto>` field, while `CharacterDtoResultDto` and
`SessionDtoResultDto` use `StartArray("messages")` for the same field
shape. The generator defaults to `RegV` for all array fields (majority
pattern observed). **This must be verified in Workbench before trusting
generated array (de)serialization** — flag in the implementation plan as
a required manual verification step, not something to resolve by reading
more code.

`allOf`-based schema composition (inheritance) is out of scope for v1 —
matches the "pragmatic subset" scope decision.

## Operation generation (paths → client + callbacks)

Confirmed native Enfusion API surface (Bohemia's public Enfusion Script
API reference, `GameLib/generated/online/{RestApi,RestContext,JsonApiStruct}.c`):

```
RestContext.GET(RestCallback cb, string request)
RestContext.POST(RestCallback cb, string request, string data)
RestContext.PUT(RestCallback cb, string request, string data)
RestContext.DELETE(RestCallback cb, string request, string data)
RestContext.SetHeaders(string definition)
RestContext.SetTimeout(int timeoutS)

JsonApiStruct.Pack()       // stage registered fields into internal JSON repr
JsonApiStruct.AsString()   // pull out the JSON string
JsonApiStruct.ExpandFromRAW(string data)  // inbound deserialize (already used today)
```

No `PATCH` verb exists on `RestContext`. OpenAPI operations with
`patch` are skipped with a generator warning in v1.

**Client shape**: one flat singleton class per your existing convention
(not a fluent indexer/RequestBuilder tree — Enforce Script's syntax and
your existing `ELIFE_Api` pattern both favor flat methods). Split across
one `modded class {Prefix}Api` file per OpenAPI `tag`, mirroring
`ELIFE_Api_Overrides.c`:

```c
// Api/ELIFE_Api_Character.c  (regenerated every run)
modded class ELIFE_Api
{
	void GetAccountCharacters(string accountId, Managed instance = null, string functionName = "")
	{
		ELIFE_CharacterDtoListResultDtoCallback cbx = new ELIFE_CharacterDtoListResultDtoCallback;
		cbx.SetCallback(instance, functionName, accountId);
		GetElifeApi().GET(cbx, string.Format("accounts/%1/characters", accountId));
	}

	void CreateCharacter(CharacterDto body, Managed instance = null, string functionName = "")
	{
		ELIFE_CharacterDtoResultDtoCallback cbx = new ELIFE_CharacterDtoResultDtoCallback;
		cbx.SetCallback(instance, functionName);
		body.Pack();
		GetElifeApi().POST(cbx, "characters", body.AsString());
	}
}
```

Path params are substituted positionally (`%1`, `%2`, ...) via
`string.Format`, matching the existing convention exactly. Query params
follow the same positional-substitution approach, appended to the path
template. Request-body params are typed by the operation's request
schema; the method calls `body.Pack(); body.AsString()` before handing it
to the verb call.

**Callback generation** — per distinct response DTO (deduped across
operations), matching the existing hand-written shape:

```c
// Api/Callbacks/ELIFE_CharacterDtoResultDtoCallback.c
class ELIFE_CharacterDtoResultDtoCallback : ELIFE_BaseRestCallback
{
	override ELIFE_EApiStatusCode ExtractData(string data, int dataSize, out JsonApiStruct resultData)
	{
		resultData = new CharacterDtoResultDto();
		resultData.ExpandFromRAW(data);
		if (!resultData)
			return ELIFE_EApiStatusCode.ERROR;
		return ELIFE_EApiStatusCode.SUCCESS;
	}
}
```

`{Prefix}BaseRestCallback` and `{Prefix}EApiStatusCode` are emitted once,
verbatim, as static boilerplate (not templated per schema) — identical to
today's hand-written `ELIFE_BaseRestCallback.c`.

## Auth / headers — deferred

`RestContext.SetHeaders(string definition)` exists but its expected
string format (delimiter between multiple headers, how to express
`Authorization: Bearer <token>`) is undocumented in the public Enfusion
API reference and no example usage was found in any searched public repo.

v1 does **not** attempt to generate header/auth code from OpenAPI
`securitySchemes`. Instead it emits an overridable extension point on the
singleton (e.g. `protected string BuildHeaders() { return ""; }`) that
hand-written code can fill in once the real format is confirmed
empirically in Workbench. Auth codegen becomes a v1.1 follow-up once that
format is known.

## File layout

Mirrors the existing repo exactly:

```
Api/
  {Prefix}Api_Base.c            # scaffolded once; never overwritten if present
  {Prefix}Api_{Tag}.c           # one per OpenAPI tag; regenerated every run
  {Prefix}BaseRestCallback.c    # static boilerplate; emitted once
Api/Callbacks/
  {Prefix}{ResponseModel}Callback.c   # one per distinct response DTO
Api/Structs/
  {ModelName}.c                 # one per schema (DTOs + enums)
```

Regenerated files get an `// <auto-generated>` header. `{Prefix}Api_Base.c`
holds hand-maintained logic (URL/config loading, constructor) today and is
only scaffolded if missing — the CLI warns and skips rather than
overwriting it on subsequent runs.

## CLI surface

```
enfusion-codegen generate <spec-path-or-url> --output <dir> --prefix ELIFE_
```

`--prefix` applies to the client singleton, callback classes, and the
base callback/status-enum — never to DTO/model class names — matching
the asymmetric prefixing already present in the hand-written code
(`ELIFE_Api`, `ELIFE_BaseRestCallback` vs. plain `CharacterDto`).

## Testing strategy

- Unit tests on the schema→model mapping layer (successor to old
  `EnfusionTypeTests`).
- Golden-file/snapshot tests: run the existing `swagger.json` test
  fixture (already present in `Code-Generator`'s test suite, and it
  happens to already describe ELifeRPG's real API) through the full
  pipeline and diff the emitted `.c` files against checked-in expected
  output. The old test suite never verified rendered output end-to-end —
  this closes that gap.
- No Workbench/engine available in this environment to confirm generated
  code actually compiles in-engine. Recommend periodic manual
  smoke-testing by dropping generated output into a real Workbench
  project, specifically to verify the two flagged open risks (array
  `RegV`/`StartArray` choice, and — once attempted — `SetHeaders` format).

## Explicitly out of scope for v1

- `allOf`/`oneOf`/`anyOf` schema composition and polymorphism.
- `multipart/form-data` request bodies.
- OpenAPI `securitySchemes` → generated auth/header code.
- `PATCH` operations (no engine support).
- Fluent indexer/RequestBuilder client shape.
