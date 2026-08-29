<!-- BEGIN ACTDIM-AGENTS-PROTOCOL root (managed by init-agents: do not edit by hand) -->
# ACTDIM-AGENTS-PROTOCOL

This repo carries its own agent context, provider-agnostically. Follow it every session, whatever tool you are.

## Scope & precedence
- Any folder may carry its own `AGENTS.md` + `.agents/`; they apply to that folder and everything under it. Use the NEAREST ones for the area you're working in; higher-level ones add broader context. On conflict, the more specific wins.
- Global/user config still applies as defaults (Claude auto-loads `~/.claude/CLAUDE.md`, Codex `~/.codex/AGENTS.md`, Antigravity `~/.gemini/config/GEMINI.md`). Precedence: nearest > higher-level > global.

## At session start: read these yourself (they are NOT auto-loaded)
Use the NEAREST `.agents/` for the area you're working in (fall back to a higher-level one if the folder has none):
1. `AGENTS.md` (nearest): conventions to follow.
2. `.agents/CONTEXT.md`: current state.
3. `.agents/ISSUES.md`: active issue board.
4. `.agents/DECISIONS.md`: don't contradict.
Also, when relevant: `.agents/VISION.md`, `.agents/GLOSSARY.md`, and the `.agents/ISSUES/<type>--<slug>.md` you'll work on. These reflect the state WHEN WRITTEN: verify any named file/API/flag against the real code first.

## Issues
- One file per issue, formatted as `.agents/ISSUES/<type>--<slug>.md` (slug = lowercase kebab-case, 2–5 words).
- Supported types (`<type>`): `feat` (feature), `bug` (bug fix), `debt` (tech debt / refactoring), `task` (general task), `docs` (documentation).
- Issue YAML front-matter: `slug`, `type`, `status` (`open` | `in-progress` | `blocked` | `done`), `priority` (`critical` | `high` | `medium` | `low`), `created`, `updated`.
- `.agents/ISSUES.md` is the compact board read every session (`## Active`, `## Backlog`, `## Done (recent)`).
- On completion, MOVE the file to `.agents/ISSUES/done/<type>--<slug>.md` and update the board.

## While working
- Follow the conventions in `AGENTS.md`.
- `DECISIONS.md` is APPEND-ONLY: add a new dated entry per non-trivial architectural decision; never edit past ones: mark a replaced one "Superseded by #N".
- Add any new/clarified domain term to `.agents/GLOSSARY.md`.
- Keep the issue you touch current (its `status`/`updated` + board line); new work found = a new issue file.

## Stage Completion Triggers
A **Stage** (or milestone phase) is a meaningful, verified unit of work. An agent MUST recognize that a Stage is complete when:
1. **Issue Acceptance Met**: An active Issue (`.agents/ISSUES/<type>--<slug>.md`) has satisfied its acceptance criteria and passes verification.
2. **Plan Milestone Reached**: A distinct phase of an implementation plan agreed with the user is complete.
3. **Explicit Request**: The user asks to wrap up, checkpoint, or complete the current stage.

## Stage & Session Wrap-up Protocol (Update in order)
When a Stage or session completes, perform the following steps:
1. **Documentation & Protocol Check**: Review if `README.md`, `AGENTS.md` (project conventions), or project guides need updates following the completed stage/task. Update them or report required doc updates.
2. **Session log**: Write a new session file `.agents/SESSIONS/<YYYY>/<YYYY-MM-DD>--<short-slug>.md` (slug 2–5 words; if it exists, suffix `-02`…). Begin with YAML front-matter (`date`, `slug`, `agent` = tool/model, `branch`, `commit`, `summary`), then a body: what changed & why, files touched, decisions (by slug/#N), issues advanced, gaps/follow-ups.
3. **CONTEXT**: Rewrite `.agents/CONTEXT.md` to the new state: a SHORT snapshot, not a log; history goes to the session file.
4. **ISSUES**: Update `.agents/ISSUES.md` (+ move any completed issue to `ISSUES/done/`).
5. **HISTORY**: Append one line to `.agents/HISTORY.md`: `<YYYY-MM-DD>: <slug>: <agent>: <summary>: <link>`.
6. **VISION**: Touch `.agents/VISION.md` only if scope/roadmap changed.

## Rules
- Windows-safe filenames: dates `YYYY-MM-DD` (no `:`), date first. Issue files keep a stable `<type>--<slug>.md` name; the only move is open → `ISSUES/done/`.
- Keep `CONTEXT.md` and `ISSUES.md` compact: they cost context every session.
- Never write secrets/credentials/tokens/keys into these files; they are committed.

<!-- END ACTDIM-AGENTS-PROTOCOL -->

## Project specifics

<!-- Fill in: what this project is, how to build / test / run, architecture map. -->

### Code style

- Always brace `if`/`else`/`for`/`foreach`/`while`/`do`/`using` etc.: never single-line or same-line bodies. Applies to every language (C#, JS/TS, …).
- No `#region`/`#endregion` (or equivalent folding directives).
- English everywhere: code, comments, doc-comments, identifiers, log/exception messages.
- **DRY & Code Reusability:** Avoid duplicating boilerplate code across modules and tests. Encapsulate setup logic and RAII scopes into shared helper classes or extension methods.
- **XML Documentation & Inheritdoc:** Place authoritative XML documentation on interfaces and abstractions (e.g. in `Abstractions`). Concrete implementing classes MUST use `/// <inheritdoc />` to maintain single-source-of-truth documentation without duplication.
- **Prefer Extension Methods:** Prefer writing extension methods over static helper methods on concrete classes to maintain interface composability and clean API design.
- **Preserve Technical Comments & No Commented-Out Code:** NEVER delete inline technical explanation comments (e.g. rationale for memory allocations, zero-copy/performance optimizations, encoding nuances, or non-obvious control flow). Integrate any method/class-level technical references or notes directly into XML doc blocks via `<remarks>` tags instead of placing raw `//` comments above `/// <summary>`. DO NOT leave commented-out code blocks in `.cs` source files: extract any useful alternative code snippet into an Issue file in `.agents/ISSUES/` for future evaluation and remove the dead code block from the `.cs` file.
- **Production-Realistic Tests:** Write tests that reflect real-world developer experience (e.g. resolving dependencies via DI containers instead of direct `new` instantiations where applicable).
- **Raw String Literals over Quote Escaping:** When writing multi-line code snippets, JSON payloads, embedded scripts, or templates containing quotation marks in source files, tests, and documentation, ALWAYS prefer raw string literals (`"""..."""`) instead of escaping quotes with backslashes (`\"`). Keep text clean, readable, and as-is without escaping.

### Before acting on a request

Don't rush to implement. First check: is it warranted (does it solve the real problem)? is it technically sound? any disputed/ambiguous points to clarify? does it fit the current architecture? does it fit the roadmap/vision? If any of these is in doubt, raise it before coding rather than implementing silently.
