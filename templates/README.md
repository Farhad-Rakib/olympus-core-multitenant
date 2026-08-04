# Olympus Templates

One `dotnet new` solution template, installed from a single package: **this repo itself**. There's
no separate template copy to keep in sync; the live repo already uses the placeholder name
`ProjectNamePlaceholder` throughout (see `claude.md` for why), so `dotnet new install .` from the
repo root installs it.

Only parameters with a real, working implementation behind them are exposed. See `TASKS.md` at the
repo root for what's deliberately not parameterized yet (ORM, auth provider, background jobs,
frontend) and why — and see `olympus-core` (a sibling template) for what a real `--orm` choice
looks like once there's a second real implementation to switch between.

Module/feature scaffolding used to be two extra item templates (`olympus-module`/`olympus-feature`)
installed alongside this one; they were dropped to keep the template surface to one command. Add a
module by hand instead — see `templates/GUIDE.md` §5, or `Application/ModuleSystem/CoreModule.cs`
and `PlatformModule.cs` for the pattern to copy. The underlying `IOlympusModule`/`ModuleHost`
framework itself is unaffected — only the code-generation shortcut is gone.

## Install

```bash
dotnet new install .   # from the repo root
```

## Generate a new solution

```bash
dotnet new olympus-multi -n CommerceOS --database postgres --cache redis --messagebroker inprocess -o ./CommerceOS
```

`-n` renames every project, namespace, and file (`ProjectNamePlaceholder` → `CommerceOS`).
`--database` is `postgres` (default) or `sqlserver` — picking one excludes the other provider's
packages, DI wiring, and migrations entirely. `--cache` is `memory` (default) or `redis`.
`--messagebroker` is `inprocess` (default) or `rabbitmq` — the `IEventBus` used by the Integration
Hub once two modules need to talk. All three parameters have two real, working implementations
behind them today.

## Uninstall

```bash
dotnet new uninstall .
```
