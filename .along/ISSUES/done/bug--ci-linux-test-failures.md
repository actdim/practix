---
protocol: along
protocol_version: "4.4.1"
slug: bug--ci-linux-test-failures
type: bug
status: done
completed: 2026-09-29
priority: high
created: 2026-09-29
updated: 2026-09-29
agent: claude
tags: [ci, github-actions, tests, linux]
blocked_by: []
related: [task--github-actions-ci-workflow]
---

# Fix CI Test Failures on Linux Runner and Add CI Badge

## Context
The GitHub Actions `CI` workflow (`ubuntu-latest`) fails on the `Run tests` step. Reproduced in `mcr.microsoft.com/dotnet/sdk:10.0` and confirmed by the CI log. Four tests fail:

1. `ActDim.Reflectron.Tests.ReflectronTests.GetPropertyGetter_Performance_OutperformsFastMember` - `Stopwatch` micro-benchmark, non-deterministic (also flaky on Windows).
2. `ActDim.Emitron.Tests.InterpolatorTests.Format_WithFormatSpecifier_FixedPointAndPercentage` and
3. `ActDim.Emitron.Tests.InterpolatorTests.Interpolate_ExtensionMethod_WithCustomFormatSpecifiers` - culture-dependent: runner uses invariant culture (`LANG=C.UTF-8`), `P0` renders `"15 %"`.
4. `ActDim.BytePath.Tests.BlobManagerTests.HierarchySeparator_Disabled_UsesHashSharding` - `:` is a valid file-name char on Linux, so `EscapeFileName` (based on `Path.GetInvalidFileNameChars()`) keeps it. Library behavior is intended (`:` is the hierarchy separator by default); only the test is OS-specific.

## Acceptance Criteria
- [x] Performance test tagged `Category=Performance` and excluded from CI via `--filter`.
- [x] Emitron format tests pin culture to `en-US` via a reusable scope helper.
- [x] BytePath test asserts OS-appropriate escaping.
- [x] CI badge added to `README.md`.
- [x] Linux container run passes.
