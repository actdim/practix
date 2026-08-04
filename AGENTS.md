<!-- BEGIN ACTDIM-AGENTS-PROTOCOL root (managed by init-agents — do not edit by hand) -->
# ACTDIM-AGENTS-PROTOCOL

This repo carries its own agent context, provider-agnostically. Follow it every session, whatever tool you are.

## Scope & precedence
- Any folder may carry its own `AGENTS.md` + `.agents/`; they apply to that folder and everything under it. Use the NEAREST ones for the area you're working in; higher-level ones add broader context. On conflict, the more specific wins.
- Global/user config still applies as defaults (Claude auto-loads `~/.claude/CLAUDE.md`, Codex `~/.codex/AGENTS.md`). Precedence: nearest > higher-level > global.

## At session start — read these yourself (they are NOT auto-loaded)
Use the NEAREST `.agents/` for the area you're working in (fall back to a higher-level one if the folder has none):
1. `AGENTS.md` (nearest) — conventions to follow.
2. `.agents/CONTEXT.md` — current state.
3. `.agents/TASKS.md` — active board.
4. `.agents/DECISIONS.md` — don't contradict.
Also, when relevant: `.agents/VISION.md`, `.agents/GLOSSARY.md`, and the `.agents/TASKS/<slug>.md` you'll work on. These reflect the state WHEN WRITTEN — verify any named file/API/flag against the real code first.

## Tasks
- One file per task, stable name `.agents/TASKS/<slug>.md` (slug = lowercase kebab-case, 2–5 words); the slug IS the id. Reference tasks by slug, never by path (a done file moves).
- `.agents/TASKS.md` is the compact board read every session.
- On completion, MOVE the file to `.agents/TASKS/done/<slug>.md` and update the board.

## While working
- Follow the conventions in `AGENTS.md`.
- `DECISIONS.md` is APPEND-ONLY: add a new dated entry per non-trivial architectural decision; never edit past ones — mark a replaced one "Superseded by #N".
- Add any new/clarified domain term to `.agents/GLOSSARY.md`.
- Keep the task you touch current (its `status`/`updated` + board line); new work found = a new task.

## When you finish a stage/session — update, in order
1. New session file `.agents/SESSIONS/<YYYY>/<YYYY-MM-DD>--<short-slug>.md` (slug 2–5 words; if it exists, suffix `-02`…). Begin with YAML front-matter (`date`, `slug`, `agent` = tool/model, `branch`, `commit`, `summary`), then a body: what changed & why, files touched, decisions (by slug/#N), tasks advanced, gaps/follow-ups.
2. Rewrite `.agents/CONTEXT.md` to the new state — a SHORT snapshot, not a log; history goes to the session file.
3. Update `.agents/TASKS.md` (+ move any done task to `TASKS/done/`).
4. Append one line to `.agents/HISTORY.md`: `<YYYY-MM-DD> — <slug> — <agent> — <summary> — <link>`.
Touch `.agents/VISION.md` only if scope/roadmap changed.

## Rules
- Windows-safe filenames: dates `YYYY-MM-DD` (no `:`), date first. Task files keep a stable slug name; the only move is open → `TASKS/done/`.
- Keep `CONTEXT.md` and `TASKS.md` compact — they cost context every session.
- Never write secrets/credentials/tokens/keys into these files; they are committed.
<!-- END ACTDIM-AGENTS-PROTOCOL -->

## Project specifics

<!-- Fill in: what this project is, how to build / test / run, architecture map. -->

### Code style

- Always brace `if`/`else`/`for`/`foreach`/`while`/`do`/`using` etc. — never single-line or same-line bodies. Applies to every language (C#, JS/TS, …).
- No `#region`/`#endregion` (or equivalent folding directives).
- English everywhere: code, comments, doc-comments, identifiers, log/exception messages.

### Before acting on a request

Don't rush to implement. First check: is it warranted (does it solve the real problem)? is it technically sound? any disputed/ambiguous points to clarify? does it fit the current architecture? does it fit the roadmap/vision? If any of these is in doubt, raise it before coding rather than implementing silently.
