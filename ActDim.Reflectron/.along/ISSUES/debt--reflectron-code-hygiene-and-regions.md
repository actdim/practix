---
protocol: along
slug: reflectron-code-hygiene-and-regions
type: debt
status: open
priority: low
created: 2026-09-15
updated: 2026-09-15
agent: antigravity
tags: [code-hygiene, guidelines, region, duplication]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Reflectron Code Hygiene, Region Removal, and Type Constant Duplication

## Overview
Review of `ActDim.Reflectron` revealed code style violations against repository guidelines (use of `#region` folding blocks) and redundant identical static type declarations.

## Identified Defects

### 1. `#region` Folding Directives in `Reflectron.Generic.cs`
In `Reflectron.Generic.cs` (line 209):
`#region Static Helpers for Type T` and `#endregion`.
Repository rules in `AGENTS.md` explicitly enforce:
"No `#region`/`#endregion` (or equivalent folding directives)."

### 2. Duplicated Type Constants in `Reflectron.Members.cs`
In `Reflectron.Members.cs` (lines 11-12):
```csharp
private static readonly Type BaseDelegateType = typeof(Delegate);
private static readonly Type DelegateType = typeof(Delegate);
```
Both fields define `typeof(Delegate)`. Different files in `ActDim.Reflectron` arbitrarily alternate between `BaseDelegateType` and `DelegateType`, creating needless duplication and confusion.

## Remediation Plan
1. Delete `#region` and `#endregion` folding directives in `Reflectron.Generic.cs`.
2. Consolidate `BaseDelegateType` and `DelegateType` into a single canonical constant in `Reflectron.Members.cs` and update references across partial classes.

