#!/usr/bin/env bash
# Adds (or checks) an EF Core migration for ONE database provider, in isolation.
#
#   scripts/ef-migration.sh add   postgres  AddSomething
#   scripts/ef-migration.sh add   sqlserver AddSomething
#   scripts/ef-migration.sh check sqlserver            # exit 1 if the model has drifted
#
# Why this exists: both providers' migrations live in one assembly, and only the Postgres set is
# compiled in the template source (the csproj removes Migrations/SqlServer/**). Running
# `dotnet ef` naively for SQL Server therefore diffs against the WRONG baseline. Worse, EF writes
# the updated ApplicationDbContextModelSnapshot.cs to the first file of that name it finds under
# the project (Migrations/Postgres/ sorts first), silently clobbering the other provider's
# snapshot. This script flips the compile exclusion, runs `dotnet ef` with the matching design-time
# provider, then puts the snapshot back where it belongs -- and always restores the csproj.
set -euo pipefail

ACTION="${1:-}"
PROVIDER="${2:-}"
NAME="${3:-}"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC="$ROOT/src"
PERSISTENCE="$SRC/ProjectNamePlaceholder.Persistence"
CSPROJ="$PERSISTENCE/ProjectNamePlaceholder.Persistence.csproj"
MIGRATIONS="$PERSISTENCE/Migrations"

case "$PROVIDER" in
  postgres)  DIR="Postgres";  OTHER="SqlServer" ;;
  sqlserver) DIR="SqlServer"; OTHER="Postgres" ;;
  *) echo "usage: $0 <add|check> <postgres|sqlserver> [MigrationName]" >&2; exit 2 ;;
esac

if [[ "$ACTION" == "add" && -z "$NAME" ]]; then
  echo "add requires a migration name" >&2; exit 2
fi

# Exactly one <Compile Remove="Migrations/<Provider>/**/*.cs" /> is expected in the csproj.
ORIGINAL_CSPROJ="$(cat "$CSPROJ")"
restore() {
  printf '%s\n' "$ORIGINAL_CSPROJ" > "$CSPROJ"
}
trap restore EXIT

# Compile only the target provider's migrations.
sed -i.bak -E "s#<Compile Remove=\"Migrations/[A-Za-z]+/\*\*/\*\.cs\" />#<Compile Remove=\"Migrations/${OTHER}/**/*.cs\" />#" "$CSPROJ"
rm -f "$CSPROJ.bak"

export OLYMPUS_DESIGN_TIME_PROVIDER="$PROVIDER"
EF=(dotnet ef --project "$PERSISTENCE" --startup-project "$PERSISTENCE")

if [[ "$ACTION" == "check" ]]; then
  "${EF[@]}" migrations has-pending-model-changes
  exit $?
fi

if [[ "$ACTION" != "add" ]]; then
  echo "unknown action '$ACTION'" >&2; exit 2
fi

SNAPSHOT_HERE="$MIGRATIONS/$DIR/ApplicationDbContextModelSnapshot.cs"
SNAPSHOT_OTHER="$MIGRATIONS/$OTHER/ApplicationDbContextModelSnapshot.cs"
OTHER_BEFORE="$(cat "$SNAPSHOT_OTHER")"

"${EF[@]}" migrations add "$NAME" --output-dir "Migrations/$DIR"

# If EF wrote the snapshot into the other provider's folder, move it back and restore the other.
if [[ "$(cat "$SNAPSHOT_OTHER")" != "$OTHER_BEFORE" ]]; then
  mv "$SNAPSHOT_OTHER" "$SNAPSHOT_HERE"
  printf '%s' "$OTHER_BEFORE" > "$SNAPSHOT_OTHER"
  echo "Relocated ApplicationDbContextModelSnapshot.cs into Migrations/$DIR/ (EF had written it to Migrations/$OTHER/)."
fi

# EF keeps whatever namespace the existing snapshot file had; make sure it matches its folder.
sed -i.bak -E "s#namespace ProjectNamePlaceholder\.Persistence\.Migrations\.[A-Za-z]+#namespace ProjectNamePlaceholder.Persistence.Migrations.${DIR}#" "$SNAPSHOT_HERE"
rm -f "$SNAPSHOT_HERE.bak"

echo "Migration '$NAME' added under Migrations/$DIR. Review it before committing."
