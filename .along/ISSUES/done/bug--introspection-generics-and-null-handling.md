---
protocol: along
protocol_version: "4.4.7"
slug: introspection-generics-and-null-handling
type: bug
status: done
completed: 2026-10-08
priority: high
created: 2026-10-08
updated: 2026-10-08
agent: antigravity
tags: [introspection, generics, null-safety, pkg-practix-common]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Fix open-generic recursion, top-level type NRE and closed generic key collisions in Introspection

## Problem Statement / Background
In `ActDim.Practix.Common.Introspection`, three distinct failure modes were identified during reflection inspection:
1. Introspecting recursive open-generic type hierarchies caused infinite recursion and stack overflow exceptions.
2. Passing null or uninitialized top-level types to introspection formatters resulted in unhandled `NullReferenceException`.
3. Closed generic type variants and array types collided within `InMemoryIntrospectionStorage` due to lack of distinct member signatures in `IntrospectionMemberId`.

## Implementation Details
1. Extended `IntrospectionMemberId` record with `MemberSignature` to uniquely differentiate constructed generic variants and multidimensional arrays.
2. Added cycle detection and recursion guards inside `TypeBaseIntrospectionInfo` and `TypeIntrospectionInfo`.
3. Hardened `IntrospectionExtensions` with null-safe member identification and defensive parameter guards.
4. Added `TryGet` support and null validation in `InMemoryIntrospectionStorage`.
5. Added unit tests in `Tests/Common.Tests/IntrospectionTests.cs` covering open-generic cycle breaking, null handling, and distinct generic member signatures.

## Acceptance Criteria
- [x] Recursive open-generic types resolve without triggering stack overflow or circular lookup deadlocks.
- [x] Top-level null types and members are safely handled without throwing `NullReferenceException`.
- [x] Distinct closed generic type definitions generate unique `IntrospectionMemberId` keys without collision in storage.
- [x] Unit test suite in `IntrospectionTests` passes with 0 failures.
