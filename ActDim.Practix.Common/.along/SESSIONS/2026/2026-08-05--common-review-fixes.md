---
protocol: along
date: 2026-08-05
slug: common-review-fixes
agent: Claude Code / claude-opus-4-8
branch: main
commit: 4995d1e
summary: "Broad critical review of ActDim.Practix.Common (dubious-value + implementation problems) plus four targeted fixes: ShortId, EnumerableExtensions.While, CallContext, DisposableAction."
milestone: v2.0.0-along-transition
issues_advanced: []
issues_completed: []
decisions: []
risks_logged: []
spikes_conducted: []
---

# Common review + targeted fixes

## What & why
Ran a broad critical review of `ActDim.Practix.Common` (practical value where dubious + implementation
problems). The review is advisory; only the four items below were actually changed this session. The rest
(MathExtensions/TaskExtensions BCL duplication, Introspection StackOverflow on self-referential types +
generic id collisions, Json converter bugs, WeakTable/StaticStringDictionary complexity, Caching stampede,
etc.) remain open and were left untouched.

### Fixes applied
- **`Extensions/MathExtensions.cs` trimmed 873 → ~290 lines.** The class was entirely unused across the
  solution and ~90% pass-throughs. Removed everything the BCL already provides (`Abs`/`Sqrt`/`Sign`/`Round`/
  `Ceiling`/`Floor`/`Truncate`/trig/`Log`/`Pow`/`Exp`/`BigMul`/`DivRem` → `Math`/`MathF`; `AtLeast`/`AtMost`
  → `Math.Max`/`Min`; `Clamp` → `Math.Clamp`; `RotateLeft`/`RotateRight` → `BitOperations`; `IsNaN` →
  `double.IsNaN`) plus the broken `Remap(byte)` (integer-division normal). Kept only the non-BCL value-adds,
  fixed and re-styled (real braces, no `#region`): `HasDecimalPart` (was inverted: now
  `value != Math.Truncate(value)`), `IsBetween`, `RoundUp`/`RoundDown(value, factor)` (round to nearest
  multiple), `To(start, bound, step)` typed ranges, `Remap(float)` (now via `Math.Clamp`), `GetValueOrDefault`
  (NaN-coalesce). Trimmed exotic integer-width overloads (kept int/long/float/double/decimal).
- **`ShortId` → stateless static on `RandomNumberGenerator.GetString`.** Was a nanoid clone over
  `System.Random` (predictable) with `IDisposable`/finalizer and three live bugs: NRE in `Dispose` on the
  custom-RNG ctor path (`_random` never initialised), off-by-one length guard (`length < 7` accepted with a
  message saying 8), and a self-contradicting alphabet guard (`c.Length > 20`). Now two static
  `Generate(...)` overloads, cryptographically strong, unbiased, no instance/disposal. Removed the seed and
  custom-`Func` constructors (meaningless with a crypto source).
- **`EnumerableExtensions.While<T>(source, Func<T,int,bool>)`**: the index `i` was never incremented, so the
  callback always saw `0` (also broke `Until` / indexed overloads that delegate here). Added `i++`.
- **`CallContext` / `CallContextProvider` rewritten** onto `AsyncLocal<ImmutableDictionary<string,object>>`.
  This is the intended tool: a standalone, provider-agnostic ambient property bag with scoped push/pop -
  a `LogContext.PushProperty` equivalent with no Serilog (or Activity) dependency. Fixes two defects: the
  restore-on-dispose bug (`oldValue = value` restored the NEW value, so overwrites never rolled back) and the
  broken isolation (a mutable `CallContext` stored in `AsyncLocal` leaked `Set` across parent/sibling flows -
  `AsyncLocal` copy-on-write only fires on `.Value` assignment). Now every `Set` assigns a new immutable dict
  to `.Value`, dispose restores from the *current* state (so unrelated keys added in between survive). Removed
  `MarshalByRefObject`. `ICallContext` contract unchanged; `CallContext` is now a stateless facade. Design
  note: for a library that may run outside ASP.NET, `Activity.Current` is not guaranteed present, so the
  corr-id stays owned by this bag; harmonise opportunistically via
  `Activity.Current?.TraceId.ToString() ?? ShortId.Generate(8)`.
- **`Disposal/DisposableAction` reworked.** The `bool _disposed` guard was non-atomic (concurrent `Dispose`
  could run the action twice) and set after invocation. Switched to `Interlocked.Exchange(ref _disposeAction,
  null)`: runs at most once, releases the captured closure, dropped the misleading `// finalizer` comments
  and the commented-out ctor. Renamed `DisposableBlock<T>` → `DisposableAction<T>` (one concept, generic
  arity instead of two different names); the carried state is now a **private** field (was public `Data`) -
  it is only needed to feed the dispose action, so exposing it added nothing and invited torn-read/locking
  questions; released to `default` on dispose (no retention). The `<T>` variant's sole remaining purpose is
  alloc-free state passing via a cached (non-capturing) delegate. Added async counterparts
  `DisposableAsyncAction` (`Func<ValueTask>`) and `DisposableAsyncAction<T>` (`IAsyncDisposable`, same atomic
  run-once + state release). Call site `LoggerProvider.cs` updated to `DisposableAction<IDisposable[]>`.

## Files touched
- `Extensions/MathExtensions.cs` (trimmed to non-BCL helpers, 873 → ~290 lines)
- `ShortId.cs` (rewritten)
- `../ActDim.Practix.Logging/LoggerProvider.cs` (call site `new ShortId().Generate(8)` → `ShortId.Generate(8)`)
- `Extensions/EnumerableExtensions.cs` (`While` index increment)
- `Messaging/CallContext.cs` (stateless facade)
- `Messaging/CallContextProvider.cs` (`AsyncLocal<ImmutableDictionary>`)
- `Disposal/DisposableAction.cs` (atomic `DisposableAction`/`DisposableAction<T>` + async `DisposableAsyncAction`/`<T>`)
- `../ActDim.Practix.Logging/LoggerProvider.cs` (`DisposableBlock<IDisposable[]>` → `DisposableAction<IDisposable[]>`)

## Verification
`ActDim.Practix.Common` builds clean (0 errors). Tests were not run for these specific changes this session.

## Known gaps / follow-ups
- Review surfaced many unaddressed issues; highest-value open ones: Introspection StackOverflow on
  self-referential type graphs + `IntrospectionMemberId` collision for constructed generics/arrays;
  `StandardJsonSerializer` (drops custom converters / `_options` in `SerializeToBytes`; Populate/Patch ignore
  `[JsonPropertyName]`); `FloatingPointConverterFactory` float NaN/Infinity → invalid JSON; `CompositeKey`
  NRE on null/default; `FinalizationObserver` finalizer with no try/catch + key resurrection.
- Dubious-value candidates for retirement/BCL replacement: `MathExtensions`, `TypeSwitch`, `NameHelper`,
  `Task*Extensions` wrappers, `Threading/ThreadSafe`, `InvalidDataException`, `WeakTable`,
  `StaticStringDictionary`.
- Consider `<Nullable>enable</Nullable>`: would have surfaced half of the NRE findings at compile time.
