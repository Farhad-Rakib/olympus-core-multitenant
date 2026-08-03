# Olympus Templates

Three `dotnet new` templates — one solution template, two item templates — all installed from a
single package: **this repo itself**. There's no separate template copy to keep in sync; the live
repo already uses the placeholder name `ProjectNamePlaceholder` throughout (see `claude.md` for
why), so `dotnet new install .` from the repo root installs all three at once.

Only parameters with a real, working implementation behind them are exposed. See `TASKS.md` at the
repo root for what's deliberately not parameterized yet (database provider, ORM, auth provider,
message broker, background jobs, frontend) and why — and see `olympus-core` (a sibling template)
for what a real `--database`/`--orm` choice looks like once there's a second real implementation to
switch between.

## Install

```bash
dotnet new install .   # from the repo root
```

## Generate a new solution

```bash
dotnet new olympus-multi -n CommerceOS --cache redis -o ./CommerceOS
```

`-n` renames every project, namespace, and file (`ProjectNamePlaceholder` → `CommerceOS`).
`--cache` is `memory` (default) or `redis` — both are real, working cache backends today.

## Scaffold a new module into an existing solution

Run from inside the generated solution's `*.Application` project directory:

```bash
dotnet new olympus-module -n Product --rootNamespace CommerceOS
```

`--rootNamespace` must match whatever `-n` you gave `olympus-multi` (defaults to
`ProjectNamePlaceholder`, which is only correct if you're scaffolding directly into this
un-renamed repo). Generates `Modules/Product/ProductModule.cs` implementing `IOlympusModule` — no
other file needs editing. `ModuleHost.DiscoverModules` picks it up automatically the next time the
app boots.

## Scaffold a feature into an existing module

```bash
dotnet new olympus-feature --module Product -n CreateProduct --rootNamespace CommerceOS
```

Generates `Modules/Product/Features/CreateProduct/` with a Request/Response/Command/Validator/
Handler (`ICommandHandler<TCommand, TResult>`, resolved via DI — no command bus). One manual step
remains: register the handler in `DependencyInjection.cs` and add a controller action, per the
generated feature's own `README.md`.

## Uninstall

```bash
dotnet new uninstall .
```
