# batch-content-delete

- status: open
- created: 2026-08-06
- updated: 2026-08-06

## Problem

`BlobManager.DeleteManyAsync` (#004) runs every key through the single-key routine: acquire the lock,
delete the content, delete the rows. On a file system that is fine. On an object store each key costs
a separate round trip, while S3 deletes up to 1000 keys in one `DeleteObjects` call and Azure batches
through `SubmitBatch`. A cleanup sweep over an expiring cache is exactly the case where this matters.

## Design

Add a batch form to the content contract, with a default implementation so no backend is forced to
implement it:

```csharp
async Task<int> DeleteAsync(IEnumerable<BlobRecord> blobRecords, CancellationToken ct)
```

Default: loop over the single-key `DeleteAsync`, same as today. An object store overrides it and
chunks by its own batch limit. Same shape as `ExistsAsync` deriving from `GetSizeAsync` (#002) — one
primitive is mandatory, the batch form is an optimisation.

The constraint from #004 stays: content is deleted only for keys whose write lock was actually
acquired. So the lock acquisition stays per key and only the content deletion batches — collect the
successfully locked records, delete their content in one call, then delete the rows. Keys that failed
to lock are skipped exactly as now.

Note this changes the failure granularity: a batch that partially fails leaves some content deleted
and some not, with all the locks held. The per-key result must be honoured, not just the count, or
rows will be deleted for content that survived.

## Done when

- [ ] batch overload with a default implementation; `FileSystemBlobDataStore` keeps the default
- [ ] `DeleteManyAsync` locks per key, then deletes content in one call
- [ ] partial batch failure does not delete rows whose content survived
- [ ] tests: bulk deletion still skips locked keys, and a partial failure leaves matching rows intact
