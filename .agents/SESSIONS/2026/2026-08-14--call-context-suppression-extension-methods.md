---
date: 2026-08-14
slug: call-context-suppression-extension-methods
agent: antigravity
branch: main
commit: pending
summary: Implemented dynamic telemetry suppression (IncludeExternalScopes, IncludeCallContext), added CallContextExtensions for ICallContext, and updated AGENTS.md conventions for extension methods.
---

# Session Log: Dynamic Telemetry Suppression & CallContext Extension Methods

## Summary of Changes
1. **Dynamic Telemetry Suppression & Options:**
   - Created `EventLoggerOptions` with `IncludeExternalScopes` and `IncludeCallContext` default flags.
   - Added `CallContextPropertyNames.IncludeExternalScopes` and `IncludeCallContext` control keys to `CallContextProperty.cs`.
   - Updated `EventLogger` to dynamically evaluate suppression flags from active `ICallContext` data during log enrichment.

2. **CallContext Extensions (`CallContextExtensions.cs`):**
   - Created extension methods `SuppressExternalScopes()` and `SuppressCallContext()` on `ICallContext`.
   - Preferred extension methods over concrete static helpers per updated project guidelines.

3. **Updated AGENTS.md Conventions:**
   - Added **Prefer Extension Methods** rule to `AGENTS.md` Code style section.

4. **Testing & Verification:**
   - Updated `LoggingTests.cs` to test dynamic suppression and DI-driven usage.
   - Confirmed all 7 unit tests pass clean (`Passed: 7, Failed: 0`).

## Files Touched
- `ActDim.Practix.Abstractions/Context/CallContextProperty.cs`
- `ActDim.Practix.Common/Context/CallContext.cs`
- `ActDim.Practix.Common/Context/CallContextExtensions.cs` [NEW]
- `ActDim.Practix.Logging/EventLoggerOptions.cs` [NEW]
- `ActDim.Practix.Logging/EventLoggerFactory.cs`
- `ActDim.Practix.Logging/EventLogger.cs`
- `ActDim.Practix.Logging/EventLoggerExtensions.cs`
- `ActDim.Practix.Logging/README.md`
- `Tests/Logging.Tests/LoggingTests.cs`
- `AGENTS.md`
