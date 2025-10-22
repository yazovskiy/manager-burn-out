#!/usr/bin/env bash
set -euo pipefail

: "${ConnectionStrings__Default:=Data Source=./data/callwellbeing.db}"
export ConnectionStrings__Default
export CALLWELLBEING_SEED=true

echo "Seeding database using connection: $ConnectionStrings__Default"

dotnet run --project src/CallWellbeing.Worker/CallWellbeing.Worker.csproj -- --seed-only
