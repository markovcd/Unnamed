# Architecture decision records

One file per decision, in Michael Nygard's format: context, decision, consequences. Numbered; the title is a sentence stating the decision. See the `adrs` skill for when to write one and how to number it.

| # | Decision |
|---|---|
| [0001](0001-record-decisions-as-adrs.md) | Decisions are recorded as ADRs in the repository |
| [0002](0002-the-journal-is-a-versioned-plain-text-file.md) | The journal is a plain-text file with a versioned, additive-only grammar |
| [0003](0003-each-commodity-balances-on-its-own.md) | Amounts are exact decimals and each commodity balances on its own |
| [0004](0004-problems-are-values-not-exceptions.md) | Reading a journal reports problems as values and never throws |
| [0005](0005-commands-are-functions-with-fixed-exit-codes.md) | Commands are functions of arguments and writers, with fixed exit codes |
| [0006](0006-no-runtime-dependencies-and-an-internal-core.md) | The product has no runtime dependencies and its types are internal |
| [0007](0007-one-gate-script-with-locked-restores.md) | One script is the gate, and restores are locked |
