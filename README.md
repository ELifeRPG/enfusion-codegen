# enfusion-codegen

Generates an Enforce Script (Arma Reforger / Enfusion) REST API client —
DTO structs, per-response callback classes, and endpoint methods on a
singleton client class — from an OpenAPI spec.

## Install

### From a published release

Releases are published to ELifeRPG's GitHub Packages NuGet feed. Unlike
npm or Docker, GitHub Packages requires authentication for NuGet even on
public repos, so add the feed as an authenticated source once (a PAT
with the `read:packages` scope is enough):

    dotnet nuget add source --username <github-username> --password <github-pat> \
      --store-password-in-clear-text --name eliferpg-github \
      "https://nuget.pkg.github.com/ELifeRPG/index.json"

    dotnet tool install -g EnfusionCodegen.Cli

### From a local build

    dotnet pack src/EnfusionCodegen.Cli/EnfusionCodegen.Cli.csproj --configuration Release --output ./out
    dotnet tool install -g EnfusionCodegen.Cli --add-source ./out

## Usage

    enfusion-codegen generate ./openapi.json --output ./out --prefix ELIFE_

- `--prefix` names every generated type, including schema structs and enums,
  the client singleton, callback classes, and the base callback/status-enum
  (e.g. `ELIFE_CharacterDto`, `ELIFE_Api`,
  `ELIFE_CharacterDtoCallback`, `ELIFE_BaseRestCallback`).
- `Api/{prefix}Api_Base.c` is only scaffolded if it doesn't already
  exist — it's meant to hold hand-written config/bootstrap logic and is
  never overwritten by later runs.
- Everything else under `Api/`, `Api/Callbacks/`, and `Api/Structs/` is
  fully regenerated on every run.

## Known limitations (v1)

- No `allOf`/`oneOf`/`anyOf` schema composition or polymorphism support.
- No `multipart/form-data` request bodies.
- Header parameters (`"in": "header"`) become method parameters, named
  as identifiers (`Idempotency-Key` → `idempotencyKey`). The method takes
  the context from `GetElifeApi()` and calls
  `ctx.SetHeaders("Content-Type,application/json,<name>,<value>,…")` right
  before the request; an empty string header is left out. The next
  `GetElifeApi()` resets the shared context's headers to `Content-Type`
  only. That relies on `SetHeaders` applying to the request queued right
  after it, which has not been checked in Workbench yet. Header values
  can't contain a comma, since `SetHeaders` separates names and values
  with commas. Other headers set on the context by hand (e.g. in a
  customised `GetElifeApi()`) are replaced for that one call.
- No generated auth code.
- `PATCH` operations are skipped (the engine has no `PATCH` verb).
- Bodiless `POST`/`PUT`/`DELETE` calls pass an empty string literal as
  the `data` argument (e.g. `GetElifeApi().POST(cbx, "path", "")`) on
  the assumption that `RestContext`'s real signatures always take three
  arguments with no default — unverified against Workbench.
- Only `$ref`s within the same spec document are resolved; multi-file
  specs with external (cross-file) `$ref`s are not supported.
