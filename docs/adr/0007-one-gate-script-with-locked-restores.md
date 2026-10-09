# 7. One script is the gate, and restores are locked

## Context

"It passes" has to mean the same thing for an agent, a person and, later, CI. Two descriptions of what a change must pass drift apart.

## Decision

`./gate.sh` restores in locked mode, builds with warnings as errors, and runs every test. It is the truth; a narrower local run can be green about code it skipped. `Directory.Build.props` turns on package lock files, so a package version that moved fails the restore instead of building. Output goes to `gate.log`; a failing step prints its tail.

## Consequences

- CI, when added, runs `./gate.sh` and nothing else. It is not set up yet because action versions must be pinned to commit SHAs that have been checked (`TODO.md`).
- Changing a package version means regenerating the lock files in the same commit.
