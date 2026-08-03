# Olympus Templates

Three `dotnet new` templates, each verified end-to-end (installed, generated, built, and — for
`olympus-module` — confirmed to actually get picked up by `ModuleHost` discovery).

Only parameters with a real, working implementation behind them are exposed. See `TASKS.md` at the
repo root for what's deliberately not parameterized yet (database provider, ORM, auth provider,
message broker, background jobs, frontend) and why.

## Install

```bash
dotnet new install ./templates/OlympusMulti
dotnet new install ./templates/OlympusModule
dotnet new install ./templates/OlympusFeature
```

## Generate a new solution

```bash
dotnet new olympus-multi -n CommerceOS --cache redis -o ./CommerceOS
```

`-n` renames every project, namespace, and file (`OlympusCoreMultitenant` → `CommerceOS`).
`--cache` is `memory` (default) or `redis` — both are real, working cache backends today.

## Scaffold a new module into an existing solution

Run from inside the `*.Application` project directory:

```bash
dotnet new olympus-module -n Product
```

Generates `Modules/Product/ProductModule.cs` implementing `IOlympusModule` — no other file needs
editing. `ModuleHost.DiscoverModules` picks it up automatically the next time the app boots.

## Scaffold a feature into an existing module

```bash
dotnet new olympus-feature --module Product -n CreateProduct
```

Generates `Modules/Product/Features/CreateProduct/` with a Request/Response/Command/Validator/
Handler (`ICommandHandler<TCommand, TResult>`, resolved via DI — no command bus). One manual step
remains: register the handler in `DependencyInjection.cs` and add a controller action, per the
generated feature's own `README.md`.

## Uninstall

```bash
dotnet new uninstall ./templates/OlympusMulti
dotnet new uninstall ./templates/OlympusModule
dotnet new uninstall ./templates/OlympusFeature
```
