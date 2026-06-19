# Contributing

This is a reference sample, but it is wired like a real service. The same rules apply.

## Local setup

```bash
dotnet restore
dotnet build
dotnet test
```

The API runs with zero external infrastructure in demo mode (SQLite + in-memory cache + in-process messaging):

```bash
dotnet run --project src/Commerce.Api
```

Then open the API reference at `http://localhost:5080/scalar` and the demo console at `frontend/index.html`.

## Conventions

* Clean Architecture dependency rule: `Domain` depends on nothing, `Application` depends only on `Domain`, `Infrastructure` and host projects depend inward. CI fails if an inner layer references an outer one.
* No EF Core. Data access is Dapper over SQL. See `docs/adr/0001-dapper-over-ef-core.md`.
* Every command and query goes through the dispatcher and the validation and logging pipeline.
* New behaviour ships with tests. Domain and application logic get unit tests; anything touching SQL, cache, or HTTP gets an integration test.

## Commit and PR

* One logical change per PR. Keep the diff reviewable.
* CI must be green: build, unit tests, integration tests, format check, coverage.
