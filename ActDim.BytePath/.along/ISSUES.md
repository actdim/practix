# Issues   (glyphs: [ ] open  [~] in-progress  [!] blocked  [x] done)

## Active

- [ ] multipart-upload-session: persistent staged upload sessions: `BeginUploadAsync`, out-of-order `UploadPartAsync`, `CompleteUploadAsync`, and `AbortUploadAsync`; publish only complete content
- [~] add-try-create-with-conflict-behavior: `TryCreateAsync` (conflict handling) + `BlobManagerExtensions.CreateAsync` (one-shot creation from byte[]/Stream/producer delegate); see `.agents/ISSUES/add-try-create-with-conflict-behavior.md`
- [ ] move-blob-key: blob move & key rename support (`MoveAsync`) with physical content relocation, deadlock prevention, multi-datastore routing, and overwrite policies; see `.agents/ISSUES/feat--move-blob-key.md`
- [ ] repodb-sqlite-registry: refactor `SQLiteBlobRegistry` to use RepoDb (Microsoft.Data.Sqlite) instead of `sqlite-net-pcl`
## Backlog

- [x] url-safe-key-separator: URL-safe key separator (:) per RFC 3986 pchar, configurable HierarchySeparator, and Windows reserved names escaping (2026-08-28)
- [x] multi-backend: multiple IBlobDataStore instances with KeyPrefix routing and DI support (2026-08-17)
- [x] di-registration: Microsoft DI extension methods (AddBlobManager) implemented and documented (2026-08-17)
- [x] delete-blob-content: all deletion paths now remove the stored bytes too (2026-08-05)

