# Monorepo & Multi-Platform Workspace Engineering Guidelines

Strict architectural and package orchestration standards for multi-package, multi-platform monorepositories.

---

## 1. Directory Structure & Layering Architecture

Organize multi-platform repositories with strict layer separation:

```text
my-monorepo/
├── apps/                         # Concrete platform deployment heads
│   ├── web/                      # Web SPA/SSR (React / Vite / Next.js)
│   ├── desktop/                  # Desktop head (Tauri / Electron)
│   ├── mobile/                   # Mobile head (React Native / Expo)
│   └── api/                      # Backend API (Node / .NET / FastAPI)
├── packages/                     # Reusable shared libraries & contracts
│   ├── core-types/               # Pure domain interfaces, DTOs & schemas
│   ├── api-client/               # Auto-generated API client & MSW mocks
│   ├── ui-components/            # Platform-agnostic design system tokens
│   └── utils/                    # Common pure utility functions
├── pnpm-workspace.yaml           # (or root Cargo.toml, Directory.Packages.props, pyproject.toml)
└── package.json                  # Root workspace definition
```

---

## 2. Dependency Invariants & Hierarchy

1. **Unidirectional Dependency Flow**:
   - `apps/*` MAY depend on any `packages/*`.
   - `packages/*` MAY depend on lower-level `packages/*` (forming a strict Directed Acyclic Graph).
   - **Banned Peer Dependencies**: `apps/*` MUST NEVER depend directly on sibling `apps/*` (e.g. `desktop` must never import from `web`).
   - **Banned Upward Dependencies**: `packages/*` MUST NEVER import from `apps/*`.

2. **Domain Contract Isolation**:
   - Keep shared domain types, DTOs, and API schemas in pure packages (e.g. `packages/core-types`) with zero UI or heavy platform dependencies. This allows them to be shared seamlessly between `web`, `desktop`, `mobile`, and `backend`.

---

## 3. Central Package Management (CPM) Across Ecosystems

Always manage third-party dependency versions at the root level to eliminate version drift and diamond conflicts:

- **JavaScript / TypeScript (PNPM Workspaces)**:
  - Link sibling packages with `"@org/package": "workspace:*"`.
  - Fix external tool versions centrally using PNPM Catalogs (`catalog:` in `pnpm-workspace.yaml`).
  - `pnpm publish` automatically resolves `workspace:*` references to exact published semver versions.
- **.NET / C#**:
  - Place `Directory.Packages.props` in the root with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`.
  - Use `Directory.Build.props` for uniform compilation properties.
- **Rust**:
  - Centralize dependencies in root `Cargo.toml` under `[workspace.dependencies]`.
  - Crates inherit via `dependency_name = { workspace = true }`.
- **Python**:
  - Declare member paths in root `pyproject.toml` under `[tool.uv.workspace]`.

---

## 4. Build, Test & CI Scoping

1. **Change Blast Radius & Filtered Execution**:
   - In CI/CD pipelines, use workspace filtering to test and build only packages affected by changed files:
     - PNPM: `pnpm --filter ...[origin/main] test`
     - .NET: `dotnet test --filter ...` (or solution filters `.slnf`)
     - Cargo: `cargo test -p <affected_crate>`
2. **Deterministic Lockfiles**:
   - Always commit the single root lockfile (`pnpm-lock.yaml`, `Cargo.lock`, `uv.lock`) to version control.

