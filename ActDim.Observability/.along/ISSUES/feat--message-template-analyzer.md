---
protocol: along
slug: message-template-analyzer
type: feat
status: open
priority: medium
created: 2026-08-14
updated: 2026-08-14
agent: antigravity
tags: []
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Feature: Roslyn Analyzer for Message Templates

## Description
Compile-time validation of `ILogger` message templates against the tag model of `EventObservabilityBridge`. Only rules that the runtime cannot enforce belong here: the runtime collision counter (`log.collisions`) covers everything the compiler cannot see (DTO flattening, `LogEvent.ActivityTags`, ambient vs scope, non-constant templates).

## What the SDK already covers
Measured on net10.0 (SDK 10.0.103) with default analyzer settings:
- `CA2017`: placeholder/argument count mismatch, including `BeginScope`. Enabled by default, fires. Do not duplicate it.
- `CA2253` (numeric placeholders) and `CA2254` (non-constant template) exist but are silent by default.

Step 0 is therefore an `.editorconfig` entry, not code: `CA2017` → error, `CA2253` / `CA2254` → warning.

## Rules to implement
ADR-008 narrowed this issue considerably: message-template placeholders of a log call no longer become span attributes at all, so the rules about their normalization lost their basis. What is left applies to **`BeginScope` state** and to exception handling.

- `PXO002`: two names in a `BeginScope` template or DTO collapse into the same key after `EventObservabilityHelper.ToOtelName` (for example `{UserId}` and `{user_id}`, or properties `UserId` and `User_Id`). Runtime already counts these in `log.collisions`; the analyzer moves the signal to compile time.
- `PXO003`: `{@value}` / `{$value}` destructuring hints in a `BeginScope` template; the bridge strips them, so the intent is silently dropped.
- `PXO004`: an `Exception` passed as a template argument instead of the `exception` parameter, which bypasses `Activity.AddException` and therefore keeps the failure out of the trace entirely.
- `PXO005`: placeholder name does not match the argument name. Convention only, `Info`, disabled by default: `ILogger` binds arguments positionally, so a mismatch is legal code.
- Dropped: `PXO001` (reserved `log.*` namespace): with only `log.collisions` left in that namespace and no placeholder reaching the span, the rule no longer earns its keep.

## Implementation notes
- Separate `netstandard2.0` project with `EnforceExtendedAnalyzerRules`; the rest of the repository is net10.0.
- `RegisterOperationAction(OperationKind.Invocation)`: overload resolution over `LoggerExtensions` and expansion of the `params object?[]` array are needed.
- Template parser must handle `{{` / `}}`, `{Name,alignment:format}`, numeric names, `@` / `$`.
- Consumed in-repo via `ProjectReference ... OutputItemType="Analyzer" ReferenceOutputAssembly="false"`, packed under `analyzers/dotnet/cs`.
- Tests on `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`.

## Acceptance
- [ ] `.editorconfig` escalates the built-in logging rules.
- [ ] The five rules above ship with tests for positive and negative cases.
- [ ] No rule duplicates `CA2017`.
