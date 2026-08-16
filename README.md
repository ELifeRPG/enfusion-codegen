# enfusion-codegen

Generates an Enforce Script (Arma Reforger / Enfusion) REST API client —
DTO structs, per-response callback classes, and endpoint methods on a
singleton client class — from an OpenAPI spec.

Spiritual successor to [`ELifeRPG/Code-Generator`](https://github.com/ELifeRPG/Code-Generator),
extended to also generate the operations/client layer (that tool only
ever generated DTO structs from `components.schemas`).

## Install

    dotnet tool install -g EnfusionCodegen.Cli --add-source <path-to-nupkg>

## Usage

    enfusion-codegen generate ./openapi.json --output ./out --prefix ELIFE_

- `--prefix` names the client singleton, callback classes, and the base
  callback/status-enum (e.g. `ELIFE_Api`, `ELIFE_CharacterDtoCallback`,
  `ELIFE_BaseRestCallback`). DTO/model class names are never prefixed.
- `Api/{prefix}Api_Base.c` is only scaffolded if it doesn't already
  exist — it's meant to hold hand-written config/bootstrap logic and is
  never overwritten by later runs.
- Everything else under `Api/`, `Api/Callbacks/`, and `Api/Structs/` is
  fully regenerated on every run.

## Known limitations (v1)

- No `allOf`/`oneOf`/`anyOf` schema composition or polymorphism support.
- No `multipart/form-data` request bodies.
- No generated auth/header code — `RestContext.SetHeaders`'s expected
  string format is undocumented and unverified; a `BuildHeaders()`
  extension point is scaffolded instead.
- `PATCH` operations are skipped (the engine has no `PATCH` verb).
- Bodiless `POST`/`PUT`/`DELETE` calls pass an empty string literal as
  the `data` argument (e.g. `GetElifeApi().POST(cbx, "path", "")`) on
  the assumption that `RestContext`'s real signatures always take three
  arguments with no default — unverified against Workbench.
- Only `$ref`s within the same spec document are resolved; multi-file
  specs with external (cross-file) `$ref`s are not supported.
