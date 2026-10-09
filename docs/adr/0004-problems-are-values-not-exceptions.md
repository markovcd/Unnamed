# 4. Reading a journal reports problems as values and never throws

## Context

A journal is hand-written, so broken input is the ordinary case, and a user fixing a file wants every problem at once, each with a line number, not the first exception.

## Decision

`JournalParser.Parse` returns a `ParseResult`: the transactions that were sound plus a list of `Diagnostic` values (source, line, message). It never throws on any text. The same holds for amount and account parsing, which return `Result<T>`. A transaction with a malformed posting is dropped and reported once, at the line of the posting; its other postings are not blamed again. Exceptions are reserved for broken invariants, such as reading the value of a failed `Result`.

The only caught exceptions are the expected ways a file read fails (missing, unreadable, a directory); they become a diagnostic on line 0.

## Consequences

- Commands refuse to report on a journal with problems, except `check`, which exists to list them.
- A seeded fuzz test feeds hostile text to the parser and asserts it neither throws nor hangs.
