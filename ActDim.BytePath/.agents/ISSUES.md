# Issues   (glyphs: [ ] open  [~] in-progress  [!] blocked  [x] done)

## Active

- [ ] multipart-upload-session — persistent staged upload sessions: `BeginUploadAsync`, out-of-order `UploadPartAsync`, `CompleteUploadAsync`, and `AbortUploadAsync`; publish only complete content
- [~] add-try-create-with-conflict-behavior — `TryCreateAsync` (conflict handling) + `BlobManagerExtensions.CreateAsync` (one-shot creation from byte[]/Stream/producer delegate); see `.agents/ISSUES/add-try-create-with-conflict-behavior.md`
## Backlog

- [x] multi-backend — multiple IBlobDataStore instances with KeyPrefix routing and DI support (2026-08-17)
- [x] di-registration — Microsoft DI extension methods (AddBlobManager) implemented and documented (2026-08-17)
- [x] delete-blob-content — all deletion paths now remove the stored bytes too (2026-08-05)
