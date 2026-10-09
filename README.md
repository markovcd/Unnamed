# Tally

Plain-text double-entry bookkeeping for the command line. Keep a journal in a
text file you can read and edit by hand; `tally` checks it and reports on it.

```text
2026-01-05 * Grocery store
    expenses:food    12.50 USD
    assets:cash                      ; the amount is inferred
```

Built with C# on .NET 10. No runtime dependencies.

## Build, test, run

```sh
./gate.sh                                  # restore (locked), build, run every test
dotnet run --project src/Tally.Cli -- balance examples/household.journal
```

`./gate.sh` is the gate: the one command that decides whether the tree is good.
Day to day, run one test project with `dotnet test --project tests/Tally.Core.Tests`
and narrow with `--filter-class Tally.Core.Tests.AmountTests`.

## Commands

```text
tally check <journal>       Check a journal and report every problem.
tally balance <journal>     Show what each account holds.
tally register <journal>    List postings with a running balance.
tally accounts <journal>    List the accounts that have postings.
tally print <journal>       Write the journal in canonical form.
tally help                  Show usage.
```

`<journal>` is a file, or `-` for standard input.

| Option | Applies to | Meaning |
|---|---|---|
| `--account NAME` | balance, register | Only postings to `NAME` and the accounts beneath it. |
| `--from DATE` | balance, register | Only transactions on or after `DATE` (`YYYY-MM-DD`). |
| `--to DATE` | balance, register | Only transactions on or before `DATE`. |
| `--depth N` | balance | Roll accounts deeper than `N` segments up into their ancestor. |
| `--json` | check, balance, register, accounts | Write the answer as one line of JSON. |

Exit codes: `0` success, `1` the journal is unreadable or has problems, `2` the
command line is wrong. Answers go to standard output; problems and usage errors
go to standard error, one `file:line: message` per line.

### JSON

Quantities are strings, so no digit is lost to floating point.

```sh
$ tally balance --json --account expenses examples/household.journal
{"accounts":[{"account":"expenses:food:groceries","balance":[{"quantity":"40.00","commodity":"USD"}]}, ...],"total":[...]}
```

`check --json` answers `{"valid":…,"transactions":…,"accounts":…,"diagnostics":[{"source","line","message"}]}`
and still exits `1` when the journal has problems.

## The journal format

```text
format 1                          ; optional; must come before the first transaction

; a comment. A line starting with # is one too.

2026-01-01 * Opening balances     ; date, optional * (cleared) or ! (pending), description
    assets:bank   1000.00 USD = 1000.00 USD   ; "= amount" asserts the account's balance
    assets:cash    100.00 USD
    equity                        ; one posting may omit its amount
```

- A transaction is a `YYYY-MM-DD` line followed by indented postings (at least two).
- A posting is an account, then optionally an amount, then optionally `= amount`.
- An **account** is colon-separated segments of letters, digits, `-` and `_`.
  `assets:bank` is beneath `assets`.
- An **amount** is a number with an optional sign and decimal point, then an
  optional **commodity** of letters and digits starting with a letter: `12.50 USD`,
  `-3 EUR`, `7`. A commodity containing digits must be set apart by a space.
  Magnitudes go up to 1,000,000,000,000.
- Each commodity must sum to zero within a transaction. One posting may omit its
  amount; it takes whatever balances the rest, one posting per commodity if
  several are left over. There is no implicit conversion between commodities.
- A **balance assertion** (`= amount`) says the account itself, not its
  children, holds exactly that quantity of that commodity right after the
  posting, replaying transactions in date order (same-date transactions in file
  order).
- Everything after `;` on a header or posting line is a comment.
- Lines are at most 1024 characters; a journal is at most 16 MiB.

A journal that declares a `format` newer than this version reads is refused
whole, with a message. The format only grows: new syntax is added, existing
syntax keeps its meaning.

## Repository map

| Path | What it is |
|---|---|
| `src/Tally.Core` | Parsing, balancing, assertions, reports. No I/O. |
| `src/Tally.Cli` | The `tally` executable: argument parsing, commands, output. |
| `tests/` | One test project per source project. |
| `examples/` | Journals that ship with the repo; a test checks each one. |
| `docs/adr/` | Architecture decisions. |
| `docs/glossary.md` | The one word for each thing. |
| `AGENTS.md` | Entry point for agents working in the repo. |
