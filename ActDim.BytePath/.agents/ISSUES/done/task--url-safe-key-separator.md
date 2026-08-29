# url-safe-key-separator

- status: done
- created: 2026-08-06
- updated: 2026-08-28

## Done when

- [x] `/` and `\` are no longer implicitly meaningful in a key; the separator is explicit configuration (`HierarchySeparator`, default `':'`)
- [x] a key that round-trips through a URL path segment without encoding is documented as the supported shape, with the chosen separator stated (`:`)
- [x] distinct keys cannot resolve to the same path, including in the multi-segment branch (2026-08-06)
- [x] repeated separators and Windows reserved device names are handled
- [x] `README.md` and `DECISIONS.md` document key character expectations and storage rules
