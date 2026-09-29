---
protocol: along
protocol_version: "4.4.1"
slug: bug--flaky-ambient-context-timeout-test
type: bug
status: done
completed: 2026-09-29
priority: high
created: 2026-09-29
updated: 2026-09-29
agent: claude
tags: [ci, tests, flaky]
blocked_by: []
related: [bug--ci-linux-test-failures]
---

# Fix Flaky AmbientContext Timeout Test on CI

## Context
`ActDim.Practix.Common.Tests.Context.AmbientContextTests.WithTimeout_CancelsTokenAfterDuration_AndDisposesCleanly` failed on CI run 36562652929 (passed on the previous run with identical code). The test polled for a 50 ms timeout with a 2 s wall-clock budget; on a loaded runner the timer callback was delayed beyond it.

## Acceptance Criteria
- [x] Test awaits the token cancellation via `Register` + `TaskCompletionSource.WaitAsync(30s)` instead of polling.
- [x] Common.Tests pass locally.
