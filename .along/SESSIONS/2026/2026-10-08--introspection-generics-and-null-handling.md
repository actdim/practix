---
protocol: along
protocol_version: "4.4.7"
date: 2026-10-08
slug: introspection-generics-and-null-handling
agent: antigravity
branch: main
commit: 1e6126a
summary: Fix open-generic recursion, top-level type NRE and closed generic key collisions in Introspection
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: [bug--introspection-generics-and-null-handling]
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session: Introspection generics and null handling

## Summary
Fix open-generic recursion, top-level type NRE and closed generic key collisions in Introspection

## Decisions
- None (confirmed at wrap: no architectural decisions).

## Blackboard Record

Execution mode: direct; plan revision 1; approved: true.

### Attributed Files

| Path | Kind | Edits | Last edit | Sessions |
| --- | --- | --- | --- | --- |
| `ActDim.Practix.Common/Introspection/Extensions/IntrospectionExtensions.cs` | source | 3 | 2026-10-08T11:05:21Z | antigravity--20e22135-e1e4-43b0-a9c2-d9788fa4a401 |
| `ActDim.Practix.Common/Introspection/InMemoryIntrospectionStorage.cs` | source | 1 | 2026-10-08T11:05:44Z | antigravity--20e22135-e1e4-43b0-a9c2-d9788fa4a401 |
| `ActDim.Practix.Common/Introspection/IntrospectionInfo.cs` | source | 1 | 2026-10-08T11:02:31Z | antigravity--20e22135-e1e4-43b0-a9c2-d9788fa4a401 |
| `ActDim.Practix.Common/Introspection/IntrospectionMemberId.cs` | source | 1 | 2026-10-08T11:02:21Z | antigravity--20e22135-e1e4-43b0-a9c2-d9788fa4a401 |
| `ActDim.Practix.Common/Introspection/TypeBaseIntrospectionInfo.cs` | source | 1 | 2026-10-08T11:03:49Z | antigravity--20e22135-e1e4-43b0-a9c2-d9788fa4a401 |
| `ActDim.Practix.Common/Introspection/TypeIntrospectionInfo.cs` | source | 1 | 2026-10-08T11:04:23Z | antigravity--20e22135-e1e4-43b0-a9c2-d9788fa4a401 |
| `ActDim.Practix.Common/docs/topic--concurrent-collections.md` | docs | 1 | 2026-10-08T11:10:53Z | antigravity--20e22135-e1e4-43b0-a9c2-d9788fa4a401 |
| `ActDim.Practix.Common/docs/topic--extensions-and-utilities.md` | docs | 1 | 2026-10-08T11:07:38Z | antigravity--20e22135-e1e4-43b0-a9c2-d9788fa4a401 |
| `Tests/Common.Tests/IntrospectionTests.cs` | source | 1 | 2026-10-08T11:06:40Z | antigravity--20e22135-e1e4-43b0-a9c2-d9788fa4a401 |

### Plan

#### Living Plan: introspection-generics-and-null-handling

Title: Fix open-generic recursion, top-level type NRE and closed generic key collisions in Introspection

##### Steps
- [x] Step 1: Add MemberSignature parameter to IntrospectionMemberId record to differentiate constructed generic types and arrays
- [x] Step 2: Implement thread-safe and re-entrant cycle breaking in TypeBaseIntrospectionInfo and TypeIntrospectionInfo
- [x] Step 3: Implement safe GetIntrospectionMemberId and null-safe formatters in IntrospectionExtensions
- [x] Step 4: Update InMemoryIntrospectionStorage with TryGet and null validation
- [x] Step 5: Add comprehensive unit tests in IntrospectionTests and verify full test suite passes

### Execution Trace

#### Execution Trace: introspection-generics-and-null-handling
- 2026-10-08T11:02:21Z edit ActDim.Practix.Common/Introspection/IntrospectionMemberId.cs
- 2026-10-08T11:02:31Z edit ActDim.Practix.Common/Introspection/IntrospectionInfo.cs
- 2026-10-08T11:03:49Z edit ActDim.Practix.Common/Introspection/TypeBaseIntrospectionInfo.cs
- 2026-10-08T11:04:23Z edit ActDim.Practix.Common/Introspection/TypeIntrospectionInfo.cs
- 2026-10-08T11:05:21Z edit ActDim.Practix.Common/Introspection/Extensions/IntrospectionExtensions.cs (x3)
- 2026-10-08T11:05:44Z edit ActDim.Practix.Common/Introspection/InMemoryIntrospectionStorage.cs
- 2026-10-08T11:06:40Z edit Tests/Common.Tests/IntrospectionTests.cs
- 2026-10-08T11:07:38Z edit ActDim.Practix.Common/docs/topic--extensions-and-utilities.md
- 2026-10-08T11:09:01Z test pass (along test)
- 2026-10-08T11:09:35Z plan approved (along plan approve)
- 2026-10-08T11:10:17Z test pass (Wrap Quality Gate)
- 2026-10-08T11:10:53Z edit ActDim.Practix.Common/docs/topic--concurrent-collections.md
- 2026-10-08T11:11:20Z test pass (Wrap Quality Gate)
