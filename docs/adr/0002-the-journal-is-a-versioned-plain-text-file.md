# 2. The journal is a plain-text file with a versioned, additive-only grammar

## Context

Bookkeeping data is the one thing a user cannot regenerate, and it outlives the program that reads it. It has to be readable without the tool, diffable, and safe to keep in version control. The alternatives were a database (opaque, needs migrations) and a structured format such as JSON or YAML (verbose to write by hand for a line-oriented domain).

## Decision

The source of truth is a line-oriented text file in the style of Ledger and hledger: a dated header line, then indented postings. The grammar is in the README.

- An optional `format N` directive, before the first transaction, stamps the version. This reader understands format 1. A journal declaring a newer format is refused whole with a message, never half-read.
- The grammar only grows. New syntax may be added; existing syntax keeps its meaning. A change an old reader would get wrong raises the format number and carries an upgrade step and a fixture of the old shape.
- `tally print` writes canonical text that parses back to the same journal, and says `format 1`.
- Reading is bounded: lines up to 1024 characters, 1000 postings per transaction, 16 MiB per journal, magnitudes up to 10^12.

## Consequences

- Users can keep journals in git and edit them anywhere.
- Every shipped example in `examples/` is loaded by a test, so a grammar change cannot silently orphan them.
- The bounds are part of the contract; raising one is allowed, lowering one is a break.
