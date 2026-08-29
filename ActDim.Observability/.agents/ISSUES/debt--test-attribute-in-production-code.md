---
slug: test-attribute-in-production-code
type: debt
status: open
priority: medium
created: 2026-08-15
updated: 2026-08-15
---

# Debt: Production Code Matches a Test-Only Attribute by Name

## Description
`EventObservabilityLoggerFactory.ResolveProviderAlias` resolves provider aliases by comparing the attribute type name against a hard-coded list that includes a test type ([EventObservabilityLoggerFactory.cs:93](../../ActDim.Observability/EventObservabilityLoggerFactory.cs#L93)):

```csharp
.FirstOrDefault(a => a.GetType().Name == "ProviderAliasAttribute" || a.GetType().Name == "TestProviderAliasAttribute");
```

A test detail leaked into the library. `EventObservabilityOptions.RegisterProviderAlias` exists precisely so tests can declare aliases without the library knowing about them.

## Proposal
Drop the `TestProviderAliasAttribute` branch and have the tests register their aliases through `RegisterProviderAlias`. Matching by type name rather than by type is itself a workaround for `ProviderAliasAttribute` living in a package the library does not reference: worth a comment stating that, so the remaining string comparison does not look accidental.

## Acceptance
- [ ] No test type names appear in the library.
- [ ] Alias resolution tests use `RegisterProviderAlias` and still pass.
