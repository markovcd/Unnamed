# 1. Decisions are recorded as ADRs in the repository

## Context

Agents and people both work in this repository, and a session does not remember the last one. A shape that looks arbitrary gets "fixed", and a declined refactor gets proposed again.

## Decision

A change that is a decision rather than a fix gets an ADR under `docs/adr/`: something a later reader would otherwise undo, or a refactor someone will propose again. Format is Nygard's: context, decision, consequences. Files are numbered and listed in `docs/adr/README.md`.

## Consequences

- Before proposing a refactor, read the ADRs (`adrs` skill). Where an ADR and the code disagree, that is a finding.
- A day-old ADR is rewritten in place; an older one gets a dated amendment or a superseding ADR.
