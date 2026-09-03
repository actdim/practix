<!-- BEGIN ALONG-PROTOCOL root (managed by along-init - do not edit by hand) -->
# ALONG-PROTOCOL v2.2.18

This repo carries its own agent context, provider-agnostically. Follow it every session, whatever tool you are.

## Scope, Precedence & Subproject / Submodule Placement
- **Nearest Context Boundary**: Any folder may carry its own `AGENTS.md` + `.along/`; they apply to that folder and everything under it. Use the NEAREST ones for the area you're working in; higher-level ones add broader context. On conflict, the more specific wins.
- **Strict Subproject & Submodule Localization**:
  - In modular repositories, monorepos, Git submodules, or symlinked folders (e.g. `packages/*`, `libs/*`, `modules/*`, `Common/*`):
    - **Entity Anchoring**: All entity creation and lifecycle updates (`.along/ISSUES/`, `ISSUES.md`, `SESSIONS/`, `DECISIONS.md`, `HISTORY.md`, `docs/`) MUST be created in the **NEAREST `.along/`** directory corresponding to the specific component/subproject being modified.
    - **Submodule Isolation**: When fixing a bug, refactoring, or adding a feature to a Git submodule or symlinked utility library, the issue (`ISSUES/<type>--<slug>.md`), session log (`SESSIONS/`), ADR (`DECISIONS.md`), and history line (`HISTORY.md`) MUST be recorded directly in that submodule's `.along/`.
    - **Parent Orchestration**: The root workspace `.along/` is strictly reserved for whole-solution orchestration, top-level integration tasks, and cross-package architectural ADRs. Parent issues may reference subproject issue canonical keys (e.g. `[pkg-auth:feat--token-refresh]`), but MUST NOT absorb subproject internal entity history.
    - **Anti-Root Pollution Rule**: Agents are STRICTLY FORBIDDEN from blindly dumping subproject or submodule changes into the workspace root `.along/`.
    - **Uninitialized Subprojects**: If an active subproject has its own package manifest (`package.json`, `Cargo.toml`, `pyproject.toml`, `*.csproj`) or `.git` repo but lacks `.along/`, initialize it with `/along-init` in that folder before recording entities.
- **Precedence**: Nearest `.along/` > higher-level `.along/` > global config (`~/.claude/CLAUDE.md`, `~/.codex/AGENTS.md`, `~/.gemini/config/GEMINI.md`).

## At session start - read these yourself (they are NOT auto-loaded)
Use the NEAREST `.along/` for the area you're working in (fall back to a higher-level one if the folder has none):
1. `AGENTS.md` (nearest) - conventions to follow.
2. `.along/ISSUES.md` - active issue board (or query `/along-kb-search`).
3. `.along/DECISIONS.md` - architectural decisions & constraints.
4. Active Issue file `.along/ISSUES/<type>--<slug>.md` for your task.
Also, when relevant: `.along/VISION.md`, `.along/GLOSSARY.md`. These reflect the state WHEN WRITTEN - verify any named file/API/flag against the real code first.

## Multi-Agent & Multi-Branch Concurrency Protocol
1. **Single Source of Truth (SSOT) vs Derived Projections**:
   - **SSOT Entities**: Atomic markdown files (`.along/ISSUES/<type>--<slug>.md`, `.along/SESSIONS/<YYYY>/<date>--<slug>.md`, `docs/topic--<slug>.md`, `.along/DECISIONS.md`).
   - **Derived Projections (Compiled Views)**: `.along/ISSUES.md`, `docs/INDEX.md`, `.along/DASHBOARD.md`.
   - **Zero-Manual-Merge Rule**: When git merge conflicts occur in derived projections (`ISSUES.md`, `INDEX.md`), never resolve diffs manually. Accept either incoming change and run `/along-issue-sync` or `/along-kb-sync` to recompile the projection from source files.
2. **Append-Only Linear Merge Driver**:
   - `.along/HISTORY.md` and `.along/DECISIONS.md` are append-only. Configure `.gitattributes` with `merge=union` to allow parallel branches to append entries without git merge conflicts.
3. **Feature-Scoped Context & Blackboard Isolation**:
   - The obsolete global `CONTEXT.md` file is deleted. Context is strictly localized to:
     - The target issue file (`.along/ISSUES/<type>--<slug>.md`).
     - Session-scoped ephemeral blackboard (`.along/.session/<slug>/` in `along-team`), automatically purged on wrap-up.
     - Completed session logs in `.along/SESSIONS/<YYYY>/<date>--<slug>.md`.

## Mandatory Issue Anchoring (Issue-Driven Execution)
1. **No Code Edits Without an Issue**:
   Before modifying any source code, agents MUST identify an existing issue or create a new one in `.along/ISSUES/<type>--<slug>.md`:
   - Set `status: in-progress` and assign agent/branch metadata.
   - For multi-issue tasks, declare the parent issue (`parent: <slug>`) or list active issues in the session log (`issues_advanced: []`).
2. **Exemptions (Anti-Pollution & Token Conservation)**:
   - **Read-only / Q&A**: DO NOT create issues for questions or code exploration.
   - **Micro-edits**: 1-line typo fixes, comment adjustments, or tiny lint tweaks are recorded directly in the session log without creating an issue file.
3. **Commit & History Binding**:
   - Every commit created by `/along-commit` MUST bind to the active issue slug.

## Entity Ecosystem & Structured Metadata
All entities are designed for zero-friction auto-parsing by dashboards and tools via YAML front-matter:

### 1. Issues (`.along/ISSUES/<type>--<slug>.md`)
- **Placement**: Nearest `.along/ISSUES/`. Types: `feat`, `bug`, `debt`, `task`, `docs`.
- **Front-matter**:
  - `protocol`: `along` (mandatory protocol marker).
  - `protocol_version`: optional quoted protocol version at creation, taken from the version in this document's title.
  - `slug`: lowercase kebab-case slug (2-5 words).
  - `type`: `feat` | `bug` | `debt` | `task` | `docs`.
  - `status`: `open` | `in-progress` | `blocked` | `done`.
  - `priority`: `critical` | `high` | `medium` | `low`.
  - `created`: `YYYY-MM-DD`.
  - `updated`: `YYYY-MM-DD`.
  - `completed`: `YYYY-MM-DD` (mandatory when `status: done` / moved to `done/`).
  - `agent`: model or tool name (e.g. `antigravity`, `claude-code`).
  - `tags`: array of tags (e.g. `[mcp, protocol]`).
  - `milestone`: optional milestone slug (e.g. `v2.2.0-along`).
  - `blocked_by`: optional array of blocking entity keys/slugs (e.g. `[feat--core-parser]`).
  - `related`: optional array of associative entity keys/slugs (e.g. `[risk--api-limit]`).
  - `parent`: optional parent entity key/slug (e.g. `feat--epic-container`).
- **Entity Linking & Graph Invariance**:
  - Reference entities strictly by canonical key (`<type>--<slug>` or `<slug>`), NEVER by local file path, ensuring links survive moves into `done/`.
  - Links are unidirectional in front-matter; inverse relationships (`blocks`, `children`) and full DAGs are resolved dynamically by graph tools and dashboards.
- `.along/ISSUES.md` is the compact board read every session (`## Active`, `## Backlog`, `## Done (recent)`).
- On completion: set `status: done` and `completed: YYYY-MM-DD`, MOVE to `.along/ISSUES/done/<type>--<slug>.md`, and update `.along/ISSUES.md`.

### 2. Decisions (ADRs) (`.along/DECISIONS.md`)
- Append-only Architectural Decision Records with decentralized slug headers:
  - Header: `## ADR-YYYY-MM-DD--<slug> - <Title>`
  - Fields: `- Date: YYYY-MM-DD`, `- Status: accepted | superseded by ADR-YYYY-MM-DD--<slug>`, `- Context: ...`, `- Decision: ...`, `- Consequences: ...`
  - Slug-based headers prevent merge collisions when parallel branches record architectural decisions.

### 3. Milestones & Releases (`.along/MILESTONES/<slug>.md`)
- Group multiple issues into a release target, stage, or sprint.
- **Front-matter**: `protocol: along`, `protocol_version`, `slug`, `title`, `status` (`open` | `in-progress` | `completed`), `due_date`, `created`, `target_issues: []`, `progress_pct`.

### 4. Risks & Blockers (`.along/RISKS/<slug>.md`)
- Track external dependencies, API limits, blocking ambiguities, and security flags.
- **Front-matter**: `protocol: along`, `protocol_version`, `slug`, `title`, `severity` (`critical` | `high` | `medium` | `low`), `status` (`active` | `mitigated` | `resolved`), `owner` (`agent` | `user`), `mitigation`, `created`, `updated`.

### 5. Spikes & R&D Experiments (`.along/SPIKES/<slug>.md`)
- Exploratory spikes, benchmark experiments, and library evaluations before implementation.
- **Front-matter**: `protocol: along`, `protocol_version`, `slug`, `title`, `status` (`hypothesis` | `evaluating` | `concluded`), `hypothesis`, `outcome`, `resulting_adr`, `created`.

### 6. Checklists & Verification (`.along/CHECKLISTS/<slug>.md`)
- Reusable verification checklists for quality gates, pre-commit, and security audits.
- **Front-matter**: `protocol: along`, `protocol_version`, `slug`, `title`, `category` (`pre-commit` | `stage-completion` | `release` | `security`), `items: [{ id, text, verified: bool }]`.

### 7. Sessions (`.along/SESSIONS/<YYYY>/<YYYY-MM-DD>--<slug>.md`)
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

## Knowledge Base (KB) Management & LLM-Wiki Integration
- **Structured Knowledge Base**: Maintain active project documentation in `docs/` with standard articles:
  - `docs/INDEX.md`: Central cross-linked topic catalog and entry point (`[Title](./topic--architecture.md)`).
  - `docs/topic--architecture.md`: System components, boundaries, and data flows.
  - `docs/topic--domain-model.md`: Domain concepts, business logic, and terms.
  - `docs/topic--setup-and-workflow.md`: Build, run, test, and workflow instructions.
  - `docs/topic--<slug>.md`: Specific domain topics and module specifications.
- **Source Archival (`.archive/`)**: Processed raw sources, unmanaged notes, and drafts are archived into `.archive/` (excluded from active KB search and site generators).
- **Front-matter Schema**: Every `docs/*.md` article MUST include YAML front-matter: `protocol: along`, `protocol_version` (the current protocol version, quoted), `slug`, `title`, `type` (`topic` | `architecture` | `domain-model` | `setup-workflow` | `index`), `created`, `updated`, `tags: []`.
- **Stable Entry Point Rule**: Files outside the service directory (`README.md`, `docs/`, package manifests, external documentation) MUST NOT link directly into `.along/`, nor into legacy service paths from earlier protocol versions. Route every such reference through a stable canonical path in `docs/` (`docs/INDEX.md` or `docs/topic--<slug>.md`). The rule governs published links only: agents still read `.along/ISSUES.md` and `.along/DECISIONS.md` directly, as instructed at session start.
- **Inbound Link Rewriting Engine & Migration Invariance**: Whenever documentation schemas change, migration engines (`/along-update`, `/along-kb-sync`) MUST recursively rewrite legacy path references across all repository Markdown files before deleting legacy directories.
- **Monorepo Scope Rule**: Knowledge Base synchronization, link rewriting, and link verification operate recursively across all subprojects, packages (`packages/*`, `apps/*`), and directories.
- **Portable Markdown Links**: All internal cross-references MUST use standard relative Markdown links (`[Title](./target.md)`) for universal rendering across GitHub, GitHub Pages, IDEs, and npm.
- **Idempotent Synchronization**: Use `/along-kb-sync` to bootstrap, compile, and validate links in `docs/` and archive raw sources.
- **Strict Fact Grounding Requirement**: Agents MUST extract facts strictly from actual `README.md`, `docs/`, `package.json`, and codebase symbols. Generating generic LLM placeholders is strictly prohibited.
- **Targeted Fast Retrieval**: Agents MUST query `/along-kb-search` or `wiki_query` for concise snippets before reading whole documentation files into context.
- **Documentation Blast Radius & Code-Graph-to-Wiki Synchronization**: After non-trivial code modifications, agents MUST determine the documentation blast radius by mapping affected AST symbols and dependent modules (discovered via `code-review-graph` or code search) to corresponding Knowledge Base articles (`docs/topic--<slug>.md`) using `along-kb-search` or symbol search. All impacted topic articles MUST be updated to reflect interface, architectural, or workflow changes before completing the task.

## While working
- Follow the conventions in `AGENTS.md`.
- `DECISIONS.md` is APPEND-ONLY: add a new dated entry with slug header (`## ADR-YYYY-MM-DD--<slug>`) per non-trivial architectural decision; never edit past ones - mark a replaced one "Superseded by ADR-YYYY-MM-DD--<slug>".
- Add any new/clarified domain term to `.along/GLOSSARY.md`.
- **Context & Token hygiene**: Keep tool output lean to prevent context bloat. Use quiet flags for builds/tests (`pytest -q`, `dotnet test -v q`), filter command outputs, and inspect targeted line ranges.
- **Mandatory Agentic Code Review & Blast Radius Impact**: After completing non-trivial code modifications, agents MUST critically inspect their own diffs and evaluate systemic blast radius. Use `code-review-graph` MCP tools (`build_or_update_graph_tool`, `get_impact_radius_tool`, `get_affected_flows_tool`) to verify that downstream callers, interfaces, and dependent systems remain unbroken, edge cases and nulls are handled, and active ADRs in `.along/DECISIONS.md` are respected.
- **Targeted Knowledge Base Search**: Prioritize `along-kb-search` or `wiki_query` MCP tools for targeted searches across `docs/`, `README.md`, and `DECISIONS.md`.

## Mandatory Stage & Session Completion Checklist
When a Stage or session completes, agents MUST execute this verification checklist in exact order:
1. [ ] **Verification & Tests**: Run automated unit tests / linting / builds with quiet flags. Verify test discovery count increased appropriately and zero tests failed.
2. [ ] **File Integrity & Untracked Audit**: Inspect `git status -u` and verify that all newly created and modified files have non-zero size (`getsize > 0`), containing expected code/content without empty placeholders or corrupted bodies.
3. [ ] **Code Review & Blast Radius Assessment**:
   - Inspect git diff for unintended side effects, unhandled nulls/errors, and edge cases.
   - Verify 100% requirement coverage against the initial user request (`REQ-N`) across both core implementation and public mirror surfaces (`README.md`, `AGENTS.md`).
   - Evaluate systemic impact radius on callers/dependents using `code-review-graph` (`get_impact_radius_tool`, `get_affected_flows_tool`) or AST analysis.
   - Identify all modified subsystem symbols and impacted downstream interfaces to inform documentation updates.
   - Verify compliance with architectural decisions in `.along/DECISIONS.md`.
4. [ ] **Entity Reconciliation**:
   - Set `status: done` and `completed: YYYY-MM-DD` for finished issues; MOVE to `.along/ISSUES/done/`.
   - Update related `.along/MILESTONES/` progress percentages.
   - Resolve mitigated `.along/RISKS/` (`status: resolved` / `mitigated`).
   - Conclude active `.along/SPIKES/` and log any resulting ADR in `.along/DECISIONS.md`.
5. [ ] **Documentation Blast Radius Check & LLM-Wiki Gate**:
   - Map identified code blast radius symbols/modules to Knowledge Base topics using `along-kb-search` or symbol search in `docs/`.
   - Factually update all affected `docs/topic--*.md` articles (and `README.md` / `AGENTS.md` if public entry points or conventions changed).
   - Run `/along-kb-sync` to recompile `docs/INDEX.md`, validate link integrity, and verify zero 404 broken relative links.
6. [ ] **Session Log**: Write `.along/SESSIONS/<YYYY>/<YYYY-MM-DD>--<short-slug>.md` with complete front-matter (`protocol: along`, `issues_advanced`, `issues_completed`, `decisions`, `risks_logged`, `spikes_conducted`) and a concise Code Review & Impact summary.
7. [ ] **ISSUES Board Projection**: Run `/along-issue-sync` (or update `.along/ISSUES.md`).
8. [ ] **HISTORY**: Append one line to `.along/HISTORY.md`: `<YYYY-MM-DD> - <slug> - <agent> - <summary> - <link>`.
9. [ ] **Compaction Prompt**: Advise user to run `/compact` to free up token budget.

## Rules
- **Strict Dependency & Environment Isolation**:
  - **No Unprompted Global Installs**: Agents MUST NOT arbitrarily install system-wide or global packages (e.g. via `pip install`) when a script fails with a missing dependency.
  - **Fix the Architecture, Not the Environment**: If an engine or script fails to locate a dependency, it is an architectural defect (such as a missing `bootstrap.ensure_deps()` call or an incorrect `uv` wrapper). The agent MUST diagnose and fix the script's entry point or discuss it with the user, rather than mutating the environment to force it to pass.
- **Strict File Modification & Anti-Deletion**:
  - **Zero Unintended Deletions**: Never delete, truncate, or overwrite existing documentation, comments, planned features, or code unless explicitly instructed by the user.
  - **Mandatory Post-Batch & Destructive Operation Audit**: After running any batch replacement, migration script, refactor, or multi-file edit, agents MUST immediately execute `git diff --stat` to verify that net line counts align with expectations. Any unexpected net reduction in file size or line count must be inspected in detail before proceeding.
  - **Anti-Stub & Size Regression Invariant**: It is strictly forbidden to replace populated files, documentation, or code with placeholder templates, stubs, or summary skeletons (`// ... rest of code`).
  - **Minimal Edit Scope**: Anchor edit blocks strictly on exact single lines or minimal unique chunks.
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
  - **File Content Never Travels Through a Command Line**: Create files with the agent's file-writing tool and change them with its edit tool. NEVER carry file content in a heredoc, a `python -c` string, or any inline shell command. Such content crosses several parsers in sequence (shell, heredoc or `-c`, the language string literal, sometimes a regex), and any one of them may consume a backslash or a quote: the file is then silently corrupted, or fails with an unterminated-literal error. Symptoms observed in practice: `"

"` arriving as a real newline, an apostrophe in prose ending a quoted heredoc early, and a multi-line `python -c` losing its newlines entirely.
  - **Deterministic Entity & Command Execution**: Use deterministic subcommands via `python scripts/along_exec.py` (`issue create`, `session create`, `scratch init`) for entity work. When a script is genuinely required, write it to a file first and execute that path; never inline it. Build backslashes in code (`chr(92)`, `os.linesep`, `re.escape`) instead of escaping them through layers, and never reuse line indices captured before a list of lines was mutated.
  - **Verify Every Written File**: After writing or patching a file, confirm it still parses before moving on: `python -m compileall -q` for Python, `bash -n` for shell, `[System.Management.Automation.Language.Parser]::ParseFile()` for PowerShell, and the project's own reader for structured data. Parsing is not proof of correctness, but a file that does not parse must never be left on disk. (A fixed, content-free command like `bash -n <file>` is not what the rule above forbids: the ban is on carrying file CONTENT through a command line.)
- **Hermetic Tests (No Test May Mutate Its Own Repository)**:
  - **Fixtures, Never the Real Root**: A test MUST point every engine, script, or command it executes at a throwaway fixture (`tempfile.mkdtemp()`), never at the repository that contains the test. Engines write: they normalize front-matter, sanitize typography, rewrite links, and move entities, so a test that passes the real root can silently edit work in progress.
  - **Read-Only Access to Live State**: Tests MAY read live repository content (project memory, `docs/`, manifests) to guard against format drift, and MUST open it read-only without invoking an engine that writes.
  - **Prove It**: Keep a meta-test that snapshots `git status --porcelain -u` before and after the suite and fails if the suite dirtied the tree. "The suite is green" and "the tree is clean" must be simultaneously achievable; otherwise the suite cannot serve as a gate and CI cannot tell a real change from test noise.
- Windows-safe filenames: dates `YYYY-MM-DD` (no `:`), date first.
- Keep `ISSUES.md` compact - it costs context every session.
- Never write secrets/credentials/tokens/keys into these files; they are committed.
<!-- END ALONG-PROTOCOL -->## Project specifics

<!-- BEGIN ALONG-RULES -->
See the following engineering guidelines:
- `[languages/csharp.md](file://.along/rules/languages/csharp.md)`
- `[platforms/monorepo.md](file://.along/rules/platforms/monorepo.md)`
<!-- END ALONG-RULES -->

<!-- Fill in: what this project is, how to build / test / run, architecture map. -->

### Code style

- Always brace `if`/`else`/`for`/`foreach`/`while`/`do`/`using` etc.: never single-line or same-line bodies. Applies to every language (C#, JS/TS, ...).
- No `#region`/`#endregion` (or equivalent folding directives).
- English everywhere: code, comments, doc-comments, identifiers, log/exception messages.
- **DRY & Code Reusability:** Avoid duplicating boilerplate code across modules and tests. Encapsulate setup logic and RAII scopes into shared helper classes or extension methods.
- **XML Documentation & Inheritdoc:** Place authoritative XML documentation on interfaces and abstractions (e.g. in `Abstractions`). Concrete implementing classes MUST use `/// <inheritdoc />` to maintain single-source-of-truth documentation without duplication.
- **Prefer Extension Methods:** Prefer writing extension methods over static helper methods on concrete classes to maintain interface composability and clean API design.
- **Preserve Technical Comments & No Commented-Out Code:** NEVER delete inline technical explanation comments (e.g. rationale for memory allocations, zero-copy/performance optimizations, encoding nuances, or non-obvious control flow). Integrate any method/class-level technical references or notes directly into XML doc blocks via `<remarks>` tags instead of placing raw `//` comments above `/// <summary>`. DO NOT leave commented-out code blocks in `.cs` source files: extract any useful alternative code snippet into an Issue file in `.along/ISSUES/` for future evaluation and remove the dead code block from the `.cs` file.
- **Production-Realistic Tests:** Write tests that reflect real-world developer experience (e.g. resolving dependencies via DI containers instead of direct `new` instantiations where applicable).
- **Raw String Literals over Quote Escaping:** When writing multi-line code snippets, JSON payloads, embedded scripts, or templates containing quotation marks in source files, tests, and documentation, ALWAYS prefer raw string literals (`"""..."""`) instead of escaping quotes with backslashes (`"`). Keep text clean, readable, and as-is without escaping.

### Before acting on a request

Don't rush to implement. First check: is it warranted (does it solve the real problem)? is it technically sound? any disputed/ambiguous points to clarify? does it fit the current architecture? does it fit the roadmap/vision? If any of these is in doubt, raise it before coding rather than implementing silently.
