---
date: 2026-08-18
slug: reflectron-api-unification
agent: antigravity
branch: main
summary: Unified ActDim.Reflectron into a modern, single-brand API with IReflectron<T>, indexer, expression and string Get/Set, weak reference GC safety, Type.Reflect factories, and modular partial classes (eliminating TypeAccess and ObjectAccess).
---

# Session Summary: ActDim.Reflectron API Unification & Modernization

## Context & Objectives
- Redesign `ActDim.Reflectron` from legacy separate classes (`ObjectAccess`, `IObjectAccess`, `TypeAccess`, `TypeAccess<T>`) into a cohesive, high-performance, single-brand library centered around `Reflectron` and `IReflectron<T>`.
- Provide intuitive, fast access to read/write properties and fields by indexer `reflector["Name"]`, string name, or lambda expression `reflector.Get(x => x.Prop)`.
- Ensure memory safety via `WeakReference<T>` so long-lived reflector instances do not prevent garbage collection of target instances.
- Provide factory methods `Type.Reflect<T>()` and `Type.Reflect()` to spawn reflectors efficiently for statically known or dynamic runtime types.
- Relocate extension methods into dedicated `Extensions/` namespace/folder.

## Changes Made
1. **API Redesign & Consolidation**:
   - Deleted legacy `IObjectAccess.cs`, `ObjectAccess.cs`, and `TypeAccess.cs`.
   - Created `IReflectron<T>` with indexer `this[string name] { get; set; }`, `Get<TMember>`, `Set<TMember>`, and `GetMethod<TDelegate>` overloads.
   - Implemented `Reflectron<T>` in `Reflectron.Generic.cs` with weak-referenced target storage (`WeakReference<T>`) and static generic helpers.
   - Decomposed static `Reflectron` engine across modular partial classes:
     - `Reflectron.cs` — static entry points and `Reflectron.For` instance factories.
     - `Reflectron.Members.cs` — Expression-tree member info extraction and delegate type builders.
     - `Reflectron.Properties.cs` — Property getter and setter compilation and caching.
     - `Reflectron.Fields.cs` — Field getter and setter compilation and caching.
     - `Reflectron.Methods.cs` — Fast method call compilation and dispatching.
     - `Reflectron.Constructors.cs` — Constructor delegate generation and IL `DynamicMethod` builders.
     - `Reflectron.Events.cs` — Event adder and remover compilation.
2. **Extensions Organization**:
   - Moved `ObjectExtensions.cs` and `TypeExtensions.cs` into `ActDim.Reflectron/Extensions/`.
   - Added `type.Reflect<T>()` and `type.Reflect()` factory extensions.
3. **Tests & Verification**:
   - Renamed test fixture from `TypeAccessTests.cs` to `ReflectronTests.cs`.
   - Added comprehensive tests for indexers, value type properties/fields, validation, and performance comparison with `FastMember`.
   - Added unit tests verifying `WeakReference<T>` allows garbage collection and throws `ReflectionException` on dead targets.
4. **Documentation**:
   - Rewrote `ActDim.Reflectron/README.md` with complete usage examples (indexer, lambda expressions, factories, direct delegate caches, and weak reference safety).
   - Updated root `README.md`.

## Verification Results
- `ActDim.Reflectron.Tests`: 56/56 passing.
- Full solution test suite: 514+ passing across all assemblies with 0 failures.
