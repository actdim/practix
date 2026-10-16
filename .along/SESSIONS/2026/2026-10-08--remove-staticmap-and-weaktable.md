---
protocol: along
protocol_version: "4.4.7"
date: 2026-10-08
slug: remove-staticmap-and-weaktable
agent: antigravity
branch: main
commit: 1e6126a
summary: Completed task--remove-staticmap-and-weaktable
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: [task--remove-staticmap-and-weaktable]
decisions: []
risks_logged: []
spikes_conducted: []
---

# Session: Remove staticmap and weaktable

## Summary
Completed task--remove-staticmap-and-weaktable

## Blackboard Record

Execution mode: direct; plan revision 1; approved: true.

### Attributed Files

| Path | Kind | Edits | Last edit | Sessions |
| --- | --- | --- | --- | --- |
| `.along/ISSUES/task--remove-staticmap-and-weaktable.md` | state | 2 | 2026-10-08T10:59:45Z | antigravity--70c5c167-d64b-4f40-9698-a2aa464d3b7d |
| `ActDim.Practix.Common/ActDim.Practix.Common.csproj` | source | 1 | 2026-10-08T10:56:56Z | antigravity--70c5c167-d64b-4f40-9698-a2aa464d3b7d |
| `ActDim.Practix.Common/README.md` | source | 1 | 2026-10-08T10:57:07Z | antigravity--70c5c167-d64b-4f40-9698-a2aa464d3b7d |
| `ActDim.Practix.Common/docs/topic--architecture.md` | docs | 2 | 2026-10-08T10:58:01Z | antigravity--70c5c167-d64b-4f40-9698-a2aa464d3b7d |
| `ActDim.Practix.Common/docs/topic--concurrent-collections.md` | docs | 1 | 2026-10-08T10:58:11Z | antigravity--70c5c167-d64b-4f40-9698-a2aa464d3b7d |
| `ActDim.Practix.Common/docs/topic--domain-model.md` | docs | 1 | 2026-10-08T10:58:37Z | antigravity--70c5c167-d64b-4f40-9698-a2aa464d3b7d |
| `ActDim.Practix.Common/docs/topic--setup-and-workflow.md` | docs | 1 | 2026-10-08T10:58:45Z | antigravity--70c5c167-d64b-4f40-9698-a2aa464d3b7d |

### Plan

#### Living Plan: remove-staticmap-and-weaktable

Execution Mode: Direct

##### Goal
Remove obsolete, buggy `StaticMap.cs` (replaced by BCL `FrozenDictionary`) and `WeakTable.cs` along with their unit tests and documentation references.

##### Steps
- [x] Step 1: Remove `ActDim.Practix.Common/Collections/Specialized/StaticMap.cs` and `ActDim.Practix.Common/Collections/Concurrent/WeakTable.cs`.
- [x] Step 2: Remove test files `Tests/Common.Tests/Collections/Specialized/StaticMapTests.cs` and `Tests/Common.Tests/Collections/Specialized/StaticBiMapTests.cs`.
- [x] Step 3: Update documentation in `ActDim.Practix.Common/README.md` and `docs/` topic files.
- [x] Step 4: Reconcile associated issue `bug--weak-table-custom-comparer-hashcode-mismatch.md`.
- [x] Step 5: Verify clean build and all tests passing across the solution.

### Execution Trace

#### Execution Trace: remove-staticmap-and-weaktable
- 2026-10-08T10:56:29Z plan approved (along plan approve)
- 2026-10-08T10:56:35Z edit .along/ISSUES/task--remove-staticmap-and-weaktable.md
- 2026-10-08T10:56:56Z edit ActDim.Practix.Common/ActDim.Practix.Common.csproj
- 2026-10-08T10:57:07Z edit ActDim.Practix.Common/README.md
- 2026-10-08T10:58:01Z edit ActDim.Practix.Common/docs/topic--architecture.md (x2)
- 2026-10-08T10:58:11Z edit ActDim.Practix.Common/docs/topic--concurrent-collections.md
- 2026-10-08T10:58:37Z edit ActDim.Practix.Common/docs/topic--domain-model.md
- 2026-10-08T10:58:45Z edit ActDim.Practix.Common/docs/topic--setup-and-workflow.md
- 2026-10-08T10:59:45Z edit .along/ISSUES/task--remove-staticmap-and-weaktable.md
- 2026-10-08T10:59:50Z archived by issue done
