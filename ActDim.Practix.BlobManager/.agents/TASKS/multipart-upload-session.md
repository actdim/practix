# multipart-upload-session

## Goal

Add persistent multipart upload sessions for resumable, out-of-order uploads without exposing
partially assembled content under the final blob key.

## API direction

```csharp
Task<UploadSession> BeginUploadAsync(string key, long expectedLength, BlobStoreOptions options, CancellationToken ct);
Task UploadPartAsync(UploadSession session, long offset, Stream content, CancellationToken ct);
Task<BlobResult> CompleteUploadAsync(UploadSession session, CancellationToken ct);
Task AbortUploadAsync(UploadSession session, CancellationToken ct);
```

`PutAsync` remains the one-shot create-or-replace operation. Do not add public positioned
`WriteAsync` overloads to `IBlobDataStore`; an upload session owns the staging representation and
the receipt tracking needed to make out-of-order upload safe.

## Required behaviour

- A session has a durable identifier, target key, expected length, expiry, and received ranges.
- Parts may arrive out of order and may be retried. Define overlap/idempotency semantics and checksum
  validation before implementation.
- Store parts in staging content, never under the final key visible to readers.
- `CompleteUploadAsync` validates exact coverage of `[0, expectedLength)`, then atomically publishes
  the staged content and metadata under the final key.
- `AbortUploadAsync` and expiry cleanup remove both staging data and receipt metadata.
- Do not hold a `BlobRecord` distributed lock for the session lifetime; each request uses short-lived
  locking/transactions around its own state change.
- Backend-specific multipart facilities (S3 parts, Azure staged blocks/page writes) may implement the
  staging layer, but the session contract remains backend-neutral.

## Design work first

1. Decide session and received-range persistence schema in the registry layer.
2. Decide final-key conflict and visibility rules at completion.
3. Decide duplicate/overlapping part and checksum rules.
4. Decide expiry cleanup integration and error recovery after a publish failure.
5. Reconcile this work with `content-hash`, which already identifies hash finalisation as a
   multi-step-upload concern.

## Acceptance tests

- Parts received out of order publish the correctly assembled blob only after completion.
- A missing range makes completion fail and keeps the final blob unchanged.
- Retrying a part has defined idempotent behaviour.
- Expired and aborted sessions leave no staging data or session records.
- A reader never observes partially uploaded final content.
