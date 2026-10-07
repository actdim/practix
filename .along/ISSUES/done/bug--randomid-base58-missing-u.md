---
protocol: along
protocol_version: "4.4.6"
slug: randomid-base58-missing-u
type: bug
status: done
completed: 2026-10-07
priority: medium
created: 2026-10-07
updated: 2026-10-07
agent: antigravity
tags: [RandomId, Base58, bug]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Fix Base58 alphabet missing uppercase U in RandomId

The `RandomId.Base58Alphabet` constant contains 57 characters instead of 58 because uppercase 'U' was inadvertently omitted (tail was written as `...PQRSTVWXYZ` patterned after Crockford Base32 instead of `...PQRSTUVWXYZ`).

## Acceptance Criteria
- [ ] Add uppercase 'U' to `RandomId.Base58Alphabet` so it contains the canonical 58 Base58 characters.
- [ ] Add comprehensive unit tests in `RandomIdTests` verifying `IdAlphabetType.Base58` length, character set integrity, and exclusion of ambiguous characters ('0', 'O', 'I', 'l').
- [ ] Automated tests passing cleanly.
