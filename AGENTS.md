<!-- BEGIN ALONG-PROTOCOL root (managed by along-init - do not edit by hand) -->
# ALONG-PROTOCOL v2.0.11

This repo carries its own agent context, provider-agnostically. Follow it every session, whatever tool you are.

## Scope & precedence
- Any folder may carry its own `AGENTS.md` + `.along/`; they apply to that folder and everything under it. Use the NEAREST ones for the area you're working in; higher-level ones add broader context. On conflict, the more specific wins.
- Global/user config still applies as defaults (Claude auto-loads `~/.claude/CLAUDE.md`, Codex `~/.codex/AGENTS.md`, Antigravity `~/.gemini/config/GEMINI.md`). Precedence: nearest > higher-level > global.

## At session start - read these yourself (they are NOT auto-loaded)
Use the NEAREST `.along/` for the area you're working in (fall back to a higher-level one if the folder has none):
1. `AGENTS.md` (nearest) - conventions to follow.
2. `.along/CONTEXT.md` - current state.
3. `.along/ISSUES.md` - active issue board.
4. `.along/DECISIONS.md` - don't contradict.
Also, when relevant: `.along/VISION.md`, `.along/GLOSSARY.md`, and the `.along/ISSUES/<type>--<slug>.md` you'll work on. These reflect the state WHEN WRITTEN - verify any named file/API/flag against the real code first.

## Entity Ecosystem & Structured Metadata
All entities are designed for zero-friction auto-parsing by dashboards and tools via YAML front-matter:

### 1. Issues (`.along/ISSUES/<type>--<slug>.md`)
- **Placement**: Nearest `.along/ISSUES/`. Types: `feat`, `bug`, `debt`, `task`, `docs`.
- **Front-matter**:
  - `protocol`: `along` (mandatory protocol marker).
  - `slug`: lowercase kebab-case slug (2-5 words).
  - `type`: `feat` | `bug` | `debt` | `task` | `docs`.
  - `status`: `open` | `in-progress` | `blocked` | `done`.
  - `priority`: `critical` | `high` | `medium` | `low`.
  - `created`: `YYYY-MM-DD`.
  - `updated`: `YYYY-MM-DD`.
  - `completed`: `YYYY-MM-DD` (mandatory when `status: done` / moved to `done/`).
  - `agent`: model or tool name (e.g. `antigravity`, `claude-code`).
  - `tags`: array of tags (e.g. `[mcp, protocol]`).
  - `milestone`: optional milestone slug (e.g. `v2.0.0-along`).
  - `blocked_by`: optional array of blocking entity keys/slugs (e.g. `[feat--core-parser]`).
  - `related`: optional array of associative entity keys/slugs (e.g. `[risk--api-limit]`).
  - `parent`: optional parent entity key/slug (e.g. `feat--epic-container`).
- **Entity Linking & Graph Invariance**:
  - Reference entities strictly by canonical key (`<type>--<slug>` or `<slug>`), NEVER by local file path, ensuring links survive moves into `done/`.
  - Links are unidirectional in front-matter; inverse relationships (`blocks`, `children`) and full DAGs are resolved dynamically by graph tools and dashboards.
- `.along/ISSUES.md` is the compact board read every session (`## Active`, `## Backlog`, `## Done (recent)`).
- On completion: set `status: done` and `completed: YYYY-MM-DD`, MOVE to `.along/ISSUES/done/<type>--<slug>.md`, and update `.along/ISSUES.md`.

### 2. Milestones & Releases (`.along/MILESTONES/<slug>.md`)
- Group multiple issues into a release target, stage, or sprint.
- **Front-matter**: `protocol: along`, `slug`, `title`, `status` (`open` | `in-progress` | `completed`), `due_date`, `created`, `target_issues: []`, `progress_pct`.

### 3. Risks & Blockers (`.along/RISKS/<slug>.md`)
- Track external dependencies, API limits, blocking ambiguities, and security flags.
- **Front-matter**: `protocol: along`, `slug`, `title`, `severity` (`critical` | `high` | `medium` | `low`), `status` (`active` | `mitigated` | `resolved`), `owner` (`agent` | `user`), `mitigation`, `created`, `updated`.

### 4. Spikes & R&D Experiments (`.along/SPIKES/<slug>.md`)
- Exploratory spikes, benchmark experiments, and library evaluations before implementation.
- **Front-matter**: `protocol: along`, `slug`, `title`, `status` (`hypothesis` | `evaluating` | `concluded`), `hypothesis`, `outcome`, `resulting_adr`, `created`.

### 5. Checklists & Verification (`.along/CHECKLISTS/<slug>.md`)
- Reusable verification checklists for quality gates, pre-commit, and security audits.
- **Front-matter**: `protocol: along`, `slug`, `title`, `category` (`pre-commit` | `stage-completion` | `release` | `security`), `items: [{ id, text, verified: bool }]`.

### 6. Sessions (`.along/SESSIONS/<YYYY>/<YYYY-MM-DD>--<slug>.md`)
- Comprehensive work session log.
- **Front-matter**: `protocol: along`, `date`, `slug`, `agent`, `branch`, `commit`, `summary`, `milestone`, `issues_advanced: []`, `issues_completed: []`, `decisions: []`, `risks_logged: []`, `spikes_conducted: []`.

## Automated Intent Recognition & Entity Heuristics (Zero Human Friction)
Agents MUST automatically detect user intent and maintain entities in the background without prompting the human to manage project tracking:

| User Trigger / Natural Prompt | Auto-Inferred Entity | Automatic Agent Action (in background) |
| :--- | :--- | :--- |
| *"Build feature X"*, *"Fix bug Y"*, *"Refactor Z"* | **`ISSUE`** | Auto-create `.along/ISSUES/<type>--<slug>.md` & add to `ISSUES.md`. On completion, set `status: done`, `completed: YYYY-MM-DD` & move to `done/`. |
| *"API rate limit hit"*, *"Waiting for API key"*, *"Blocked on X"* | **`RISK / BLOCKER`** | Auto-create `.along/RISKS/<slug>.md` (`status: active`), mark related issue as `status: blocked`. |
| *"Compare library A vs B"*, *"Benchmark SQLite vs DuckDB"*, *"Test if X works"* | **`SPIKE`** | Auto-create `.along/SPIKES/<slug>.md`. After testing, document outcome & generate ADR in `DECISIONS.md` if an architectural choice was made. |
| *"Sprint goal"*, *"Preparing Release v2.0"*, *"Target for next milestone"* | **`MILESTONE`** | Auto-create `.along/MILESTONES/<slug>.md` and link newly created issues via `milestone: <slug>`. |
| *"I'm done for today"*, *"Wrap up"*, or Stage Completion | **`SESSION & CHECKLIST`** | Execute mandatory stage wrap-up checklist, compile `.along/SESSIONS/`, and update compact boards. |

### Anti-Pollution & Entity Filtering Rules
To keep `.along/` lean and avoid token bloat:
1. **Simple Q&A ("How does function X work?")**: Read-only, DO NOT create issues or entity files.
2. **Micro-edits (1-line typo fix, comment change)**: Record directly in the session log; DO NOT create an issue file.
3. **Non-trivial code changes (new logic, bug fixes, refactoring)**: ALWAYS ensure an `ISSUE` exists and tracks progress.

## Knowledge Base (KB) Management
- **Structured Knowledge Base**: Maintain project documentation in `.along/KB/` (or `docs/`) with standard articles:
  - `INDEX.md`: Central cross-linked topic map (`[[link]]`).
  - `01-architecture.md`: System components, boundaries, and data flows.
  - `02-domain-model.md`: Domain concepts, business logic, and terms.
  - `03-setup-and-workflow.md`: Build, run, test, and workflow instructions.
- **Front-matter Schema**: Every `.along/KB/*.md` article MUST include YAML front-matter: `protocol: along`, `slug`, `title`, `type` (`topic` | `architecture` | `domain-model` | `setup-workflow` | `index`), `created`, `updated`, `tags: []`.
- **Bootstrapping**: Use `/along-init-kb` to bootstrap or refresh `.along/KB/` from existing `README.md`, `AGENTS.md`, human `docs/`, and codebase analysis.
- **Strict Fact Grounding Requirement**: Agents MUST extract facts strictly from actual `README.md`, `docs/`, `package.json`, and codebase symbols. Generating generic LLM placeholders is strictly prohibited.
- **Maintenance**: Update corresponding articles in `.along/KB/` and run `/along-sync-kb` when implementing non-trivial architectural changes.

## While working
- Follow the conventions in `AGENTS.md`.
- `DECISIONS.md` is APPEND-ONLY: add a new dated entry per non-trivial architectural decision; never edit past ones - mark a replaced one "Superseded by #N".
- Add any new/clarified domain term to `.along/GLOSSARY.md`.
- **Context & Token hygiene**: Keep tool output lean to prevent context bloat. Use quiet flags for builds/tests (`pytest -q`, `dotnet test -v q`), filter command outputs, and inspect targeted line ranges.
- **Mandatory Agentic Code Review & Blast Radius Impact**: After completing non-trivial code modifications, agents MUST critically inspect their own diffs and evaluate systemic blast radius. Use `code-review-graph` MCP tools (`build_or_update_graph_tool`, `get_impact_radius_tool`, `get_affected_flows_tool`) to verify that downstream callers, interfaces, and dependent systems remain unbroken, edge cases and nulls are handled, and active ADRs in `.along/DECISIONS.md` are respected.
- **Hybrid Knowledge Base Search (KB)**: Prioritize `along-search-kb` or `wiki-llm` MCP tools for targeted searches across `.along/`, `docs/`, `wiki/`, `README.md`.

## Mandatory Stage & Session Completion Checklist
When a Stage or session completes, agents MUST execute this verification checklist in exact order:
1. [ ] **Verification & Tests**: Run automated unit tests / linting / builds with quiet flags.
2. [ ] **Code Review & Blast Radius Assessment**:
   - Inspect git diff for unintended side effects, unhandled nulls/errors, and edge cases.
   - Evaluate systemic impact radius on callers/dependents using `code-review-graph` or AST analysis.
   - Verify compliance with architectural decisions in `.along/DECISIONS.md`.
3. [ ] **Entity Reconciliation**:
   - Set `status: done` and `completed: YYYY-MM-DD` for finished issues; MOVE to `.along/ISSUES/done/`.
   - Update related `.along/MILESTONES/` progress percentages.
   - Resolve mitigated `.along/RISKS/` (`status: resolved` / `mitigated`).
   - Conclude active `.along/SPIKES/` and log any resulting ADR in `.along/DECISIONS.md`.
4. [ ] **Documentation Check**: Update `README.md`, `AGENTS.md` (project specifics), or `.along/KB/` if code interfaces or architecture changed.
5. [ ] **Session Log**: Write `.along/SESSIONS/<YYYY>/<YYYY-MM-DD>--<short-slug>.md` with complete front-matter (`protocol: along`, `issues_advanced`, `issues_completed`, `decisions`, `risks_logged`, `spikes_conducted`) and a concise Code Review & Impact summary.
6. [ ] **CONTEXT Snapshot**: Rewrite `.along/CONTEXT.md` to a short "you are here" snapshot (< 20 lines).
7. [ ] **ISSUES Board**: Update `.along/ISSUES.md` (keep active list lean, reflect done items).
8. [ ] **HISTORY**: Append one line to `.along/HISTORY.md`: `<YYYY-MM-DD> - <slug> - <agent> - <summary> - <link>`.
9. [ ] **Compaction Prompt**: Advise user to run `/compact` to free up token budget.

## Rules
- **Strict File Modification & Anti-Deletion**:
  - **Zero Unintended Deletions**: Never delete, truncate, or overwrite existing documentation, comments, planned features, or code unless explicitly instructed by the user.
  - **Minimal Edit Scope**: Anchor edit blocks strictly on exact single lines or minimal unique chunks.
  - **Mandatory Diff Verification**: After modifying any file, inspect the generated diff to ensure only intended lines were touched.
  - **Immediate Rollback**: If an unintended deletion or truncation is detected, restore missing lines immediately.
- **Technical Markdown & Formatting Standards**:
  - **Forbidden Characters (Clean ASCII & Invisible Character Ban)**:
    - NEVER use em-dash (U+2014), en-dash (U+2013), or math minus (U+2212) in agent responses, chat messages, code, docstrings, comments, session logs, or documentation. Use standard ASCII hyphens (`-`), colons (`:`), or parentheses `()`.
    - NEVER use typographic curly quotes or guillemets (left/right single/double quotes, low-9 quotes, angle quotation marks) in code blocks, shell commands, docstrings, chat, or YAML front-matter; use standard ASCII double (`"`) or single (`'`) quotes.
    - NEVER use unicode ellipsis character (U+2026); use standard three ASCII dots (`...`).
    - NEVER use non-breaking spaces (NBSP U+00A0, narrow NBSP U+202F) or zero-width invisible characters (ZWSP U+200B, ZWNJ, ZWJ, BOM U+FEFF); use standard ASCII spaces or omit.
    - NEVER use special bullet glyphs (U+2022, U+2023, U+2043); use standard ASCII hyphen (`-`) for lists.
  - **Explicit Code Fence Languages**: Always specify the language identifier on code fences (e.g. ```` ```bash ````, ```` ```yaml ````, ```` ```typescript ````, ```` ```python ````). Never use bare unlabelled fences.
  - **Relative & Portable Links**: Always use relative paths (`file://...` or standard markdown links) without hardcoding local absolute paths.
  - **UTF-8 Clean Encoding**: Keep all text files in clean UTF-8 without BOM.
- Windows-safe filenames: dates `YYYY-MM-DD` (no `:`), date first.
- Keep `CONTEXT.md` and `ISSUES.md` compact - they cost context every session.
- Never write secrets/credentials/tokens/keys into these files; they are committed.
<!-- END ALONG-PROTOCOL -->

## Project specifics

<!-- Fill in: what this project is, how to build / test / run, architecture map. -->

### Code style

- Always brace `if`/`else`/`for`/`foreach`/`while`/`do`/`using` etc.: never single-line or same-line bodies. Applies to every language (C#, JS/TS, …).
- No `#region`/`#endregion` (or equivalent folding directives).
- English everywhere: code, comments, doc-comments, identifiers, log/exception messages.
- **DRY & Code Reusability:** Avoid duplicating boilerplate code across modules and tests. Encapsulate setup logic and RAII scopes into shared helper classes or extension methods.
- **XML Documentation & Inheritdoc:** Place authoritative XML documentation on interfaces and abstractions (e.g. in `Abstractions`). Concrete implementing classes MUST use `/// <inheritdoc />` to maintain single-source-of-truth documentation without duplication.
- **Prefer Extension Methods:** Prefer writing extension methods over static helper methods on concrete classes to maintain interface composability and clean API design.
- **Preserve Technical Comments & No Commented-Out Code:** NEVER delete inline technical explanation comments (e.g. rationale for memory allocations, zero-copy/performance optimizations, encoding nuances, or non-obvious control flow). Integrate any method/class-level technical references or notes directly into XML doc blocks via `<remarks>` tags instead of placing raw `//` comments above `/// <summary>`. DO NOT leave commented-out code blocks in `.cs` source files: extract any useful alternative code snippet into an Issue file in `.along/ISSUES/` for future evaluation and remove the dead code block from the `.cs` file.
- **Production-Realistic Tests:** Write tests that reflect real-world developer experience (e.g. resolving dependencies via DI containers instead of direct `new` instantiations where applicable).
- **Raw String Literals over Quote Escaping:** When writing multi-line code snippets, JSON payloads, embedded scripts, or templates containing quotation marks in source files, tests, and documentation, ALWAYS prefer raw string literals (`"""..."""`) instead of escaping quotes with backslashes (`\"`). Keep text clean, readable, and as-is without escaping.

### Before acting on a request

Don't rush to implement. First check: is it warranted (does it solve the real problem)? is it technically sound? any disputed/ambiguous points to clarify? does it fit the current architecture? does it fit the roadmap/vision? If any of these is in doubt, raise it before coding rather than implementing silently.
