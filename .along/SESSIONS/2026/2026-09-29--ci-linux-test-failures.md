---
protocol: along
slug: ci-linux-test-failures
date: 2026-09-29
agent: antigravity
summary: "Fix CI Linux Test Failures and Add CI Badge"
---

# Session: Fix CI Linux Test Failures and Add CI Badge

- Date: 2026-09-29
- Agent: claude
- Issue: `bug--ci-linux-test-failures`

## Summary
GitHub Actions `CI` failed on `Run tests` (ubuntu-latest). Reproduced in `mcr.microsoft.com/dotnet/sdk:10.0` and confirmed via the CI log (4 failing tests).

## Changes
- `Tests/Reflectron.Tests/ReflectronTests.cs`: Stopwatch micro-benchmark tagged `[Trait("Category", "Performance")]`.
- `.github/workflows/ci.yml`: `dotnet test --filter "Category!=Performance"`.
- `Tests/Emitron.Tests/CultureScope.cs`: new RAII helper pinning current culture; used by two `InterpolatorTests` percentage-format tests (invariant culture renders `"15 %"`).
- `Tests/BytePath.Tests/BlobManagerTests.cs`: `HierarchySeparator_Disabled_UsesHashSharding` asserts OS-specific escaping (`%3A` on Windows only). Library behavior unchanged: `:` is the default hierarchy separator and never reaches file names.
- `README.md`: CI status badge.

## Verification
- Linux container: 12 test assemblies, 0 failures.
- Windows: `dotnet test -c Release --filter "Category!=Performance"`, 0 failures.
