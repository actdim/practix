---
date: 2026-08-11
slug: put-and-multipart
agent: Codex / GPT-5
branch: main
commit: d07ff04
summary: Renamed whole-blob WriteAsync to PutAsync and replaced raw offset-write planning with multipart upload sessions.
---

## What changed

- Renamed both whole-blob `IBlobDataStore.WriteAsync` overloads to `PutAsync` and propagated the
  change through the file-system store, pipe bridge, tests, README, and BlobManager guidance.
- Kept the behaviour unchanged: `PutAsync` still creates or truncates the destination using
  `FileMode.Create`; `AppendAsync` remains separate.
- Removed the planned `add-write-at-offset` task. Added `multipart-upload-session`, defining staged,
  durable begin/part/complete/abort semantics for resumable out-of-order uploads.
- Recorded ADR #009: final content must be published only after the session validates complete range
  coverage, rather than exposing a partially assembled file under the final key.

## Verification

- `dotnet test Tests/BlobManager.Tests/ActDim.Practix.BlobManager.Tests.csproj --no-restore`
- Result: 64 passed, 0 failed.

## Files touched

- `IBlobDataStore.cs`, `FileSystemBlobDataStore.cs`, `ProducerStreamBridge.cs`
- `Tests/BlobManager.Tests/BlobManagerTests.cs`
- `README.md`, `AGENTS.md`
- `.agents/DECISIONS.md`, `.agents/TASKS.md`, `.agents/TASKS/multipart-upload-session.md`,
  `.agents/VISION.md`, `.agents/CONTEXT.md`

## Follow-up

- Design and implement `multipart-upload-session`; reconcile its checksum/finalisation rules with
  `content-hash` before adding an API.
