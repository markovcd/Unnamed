# 6. The product has no runtime dependencies and its types are internal

## Context

A package is a long-term cost: updates, licenses, supply chain. The product needs parsing, decimal arithmetic, JSON output and argument handling, all of which the .NET base library covers. A public library API would also be a contract to keep.

## Decision

- The shipped assemblies reference no packages. Arguments are parsed by a page of code; JSON is written with `Utf8JsonWriter`.
- Test projects use xunit v3 on Microsoft Testing Platform, the one package in the tree.
- Every type in `Tally.Core` is `internal`. The `tally` executable and the test projects see it through `InternalsVisibleTo`. There is no interface without a second implementation.

## Consequences

- The Core assembly is not a published library yet. Making it one means choosing a public surface and recording that here.
- Builds are reproducible from the SDK alone, apart from the test framework.
