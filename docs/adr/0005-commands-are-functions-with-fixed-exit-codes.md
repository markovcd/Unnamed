# 5. Commands are functions of arguments and writers, with fixed exit codes

## Context

Most use of this tool will be by scripts and agents. They need to run it, read the answer and tell success from failure without scraping prose.

## Decision

- `Application.Run(args, input, output, error)` returns an exit code and touches no process-wide state, so tests call it in-process.
- A command is a record in a table (`Commands.All`) naming its options and a function from the parsed command line, the parsed journal and a writer to an exit code. The parser, the usage text and the README test all read the table.
- Exit codes: `0` success, `1` the journal is unreadable or has problems, `2` the command line is wrong. Each means one thing.
- Standard output carries only the answer. Problems and usage errors go to standard error as `file:line: message`.
- `--json` writes one line of JSON. `check --json` writes its diagnostics to standard output and still exits `1`.
- An option a command does not take is a usage error, not ignored.

## Consequences

- A new command is one table row and one class. A test fails if the README does not document it.
- The exit codes and JSON shapes are a contract that only grows.
