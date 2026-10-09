# 3. Amounts are exact decimals and each commodity balances on its own

## Context

Money needs exact base-10 arithmetic; `double` cannot hold 0.10. Ledger tools also infer a price when a transaction mixes commodities, which makes a typo in a quantity look like a currency exchange and silently balance.

## Decision

- A quantity is a `decimal`, with a ceiling of 10^12 in magnitude so sums over a whole journal cannot overflow.
- Within a transaction every commodity must sum to zero on its own. There is no implicit conversion and no price notation yet.
- One posting per transaction may omit its amount. It takes the negation of the rest; if several commodities are left over it expands to one posting per commodity.
- JSON output writes quantities as strings so no digit is lost to a floating-point reader.

## Consequences

- An unbalanced transaction is always an error that names the difference.
- Currency exchange cannot be recorded yet. It needs a decision on price notation (`@`) and on how reports value it; that is in `TODO.md`.
