# Glossary

One word for each thing, and the same word everywhere a person reads: the UI, the docs, ADRs, comments, test names and commit messages.

- **Say** is the word. Use it in prose, whatever the code calls the thing.
- **Code** is the name in source. Where it differs from **Say**, it differs because renaming it would break something persisted or public. A comment on it still uses **Say**.
- **Not** lists words that must not stand for the thing.

| Say | Means | Code | Not |
|---|---|---|---|
| **journal** | The text file, or its parsed form, holding every transaction. | `Journal` | ledger (that is the tool family), book, file |
| **transaction** | One dated entry whose postings sum to zero in every commodity. | `Transaction` | entry, record |
| **posting** | One account's side of a transaction. | `Posting` | line item, leg, split |
| **account** | A colon-separated path such as `assets:bank`. | `AccountName` | category, bucket |
| **commodity** | What an amount is a quantity of: `USD`, `EUR`, `BTC`. | `Commodity` | currency (a commodity need not be money), unit |
| **amount** | A quantity of one commodity. | `Amount` | money, value |
| **balance** | What an account holds: one quantity per commodity. | `Balance` | total (a total is the sum over many accounts), sum |
| **assertion** | An `= amount` on a posting claiming the account's balance. | `Assertion` | check, expectation |
| **diagnostic** | A problem found in a journal, with source and line. | `Diagnostic` | error (reserved for exceptions), warning |
| **gate** | `./gate.sh`: locked restore, build, every test. | | CI, pipeline |
