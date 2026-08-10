# History

_Index of sessions (newest last). One line per session:_
_`<YYYY-MM-DD> — <slug> — <agent> — <summary> — <relative link>`_

2026-08-05 — blob-content-lifecycle — Claude Code / claude-opus-5[1m] — Reconciled registry with data store (IsNew for missing content), made Size library-owned and store-read, deletion now removes bytes, write surface reduced to WriteAsync/AppendAsync; 8 pre-existing test failures fixed, 41/41 green — [SESSIONS/2026/2026-08-05--blob-content-lifecycle.md](SESSIONS/2026/2026-08-05--blob-content-lifecycle.md)

2026-08-06 — write-direction-inversion — Claude Code / claude-opus-5[1m] — Data-store writes now consume a stream instead of handing one out (#006), producer-delegate overload as a default interface method with a direct file-system override, zero lock timeout means "try once", consumer-facing README and the object-store constraints in VISION; 50/50 green — [SESSIONS/2026/2026-08-06--write-direction-inversion.md](SESSIONS/2026/2026-08-06--write-direction-inversion.md)

2026-08-06 — record-mutation-contract — Claude Code / claude-opus-5[1m] — Setter visibility now follows what a value is rather than whether mutation is safe, ApplyOptions moved onto BlobRecord as a write-locked Apply (#007), rationale trimmed out of public XML docs; content-hash task expanded, read-lock-persists-mutations filed; 54/54 green — [SESSIONS/2026/2026-08-06--record-mutation-contract.md](SESSIONS/2026/2026-08-06--record-mutation-contract.md)

2026-08-06 — write-options-overload — Claude Code / claude-opus-5[1m] — TryGetForWritingAsync accepts BlobStoreOptions, applied on the handed-out record and persisted on dispose (#008); registry untouched, reading still takes no options; 64/64 green — [SESSIONS/2026/2026-08-06--write-options-overload.md](SESSIONS/2026/2026-08-06--write-options-overload.md)

2026-08-10 — extend-create-task — Claude Code / sonnet-5 — Extended add-try-create-with-conflict-behavior to include one-shot CreateAsync extension methods (byte[]/Stream/producer delegate) — [SESSIONS/2026/2026-08-10--extend-create-task.md](SESSIONS/2026/2026-08-10--extend-create-task.md)
