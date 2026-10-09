#!/usr/bin/env bash
# The gate: a locked restore, a warnings-as-errors build and every test.
# Usage: ./gate.sh. Output of each step goes to gate.log; the failing step's tail is printed.
set -euo pipefail

cd "$(dirname "$0")"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export TESTINGPLATFORM_TELEMETRY_OPTOUT=1

log=gate.log
: > "$log"

step() {
  local name=$1
  shift
  echo "== $name"
  if ! "$@" >> "$log" 2>&1; then
    echo "gate failed at: $name" >&2
    tail -n 40 "$log" >&2
    exit 1
  fi
}

# --locked-mode fails the restore if a package version moved from packages.lock.json.
step "restore (locked)" dotnet restore Tally.slnx --locked-mode
step "build" dotnet build Tally.slnx --no-restore --configuration Release
step "test" dotnet test --solution Tally.slnx --no-build --configuration Release

echo "gate passed"
