---
protocol: along
protocol_version: "2.2.26"
slug: bug--fix-service-tests
type: bug
status: done
priority: high
created: 2026-09-09
updated: 2026-09-09
completed: 2026-09-09
agent: antigravity
tags: [tests, xunit-v3, service-tests, msbuild]
---

# Fix Service.Tests Project Configuration & Test Process Launcher

## Problem Description
Running `dotnet test Tests/Service.Tests/ActDim.Practix.Service.Tests.csproj` failed with catastrophic error:
`System.InvalidOperationException: Could not launch test process`

Root causes:
1. `ActDim.Practix.Service.Tests.csproj` was missing `<IsTestProject>true</IsTestProject>`, causing xUnit v3 targets to omit generating an executable output type.
2. Missing `<FrameworkReference Include="Microsoft.AspNetCore.App" />` and `<PackageReference Include="Microsoft.AspNetCore.TestHost" />`.
3. `ActDim.Practix.Service.csproj` specified `InternalsVisibleToAttribute` only as `Service.Tests` rather than `ActDim.Practix.Service.Tests`.
4. The test project contained 0 test classes.

## Acceptance Criteria
- [x] `ActDim.Practix.Service.Tests.csproj` configured with `<IsTestProject>true</IsTestProject>` and AspNetCore framework reference.
- [x] `ActDim.Practix.Service.csproj` updated with `InternalsVisibleTo("ActDim.Practix.Service.Tests")`.
- [x] Add baseline unit tests covering `ActDim.Practix.Service` (CorsPolicies, UserInfo, RegisteredClaimNames, TypeExtensions).
- [x] `dotnet test Tests/Service.Tests/ActDim.Practix.Service.Tests.csproj` passes cleanly with 0 failures (4 tests passed).
- [x] `dotnet test ActDim.Practix.sln` executes `Service.Tests` and passes cleanly.
