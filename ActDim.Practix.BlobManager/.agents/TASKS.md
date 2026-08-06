# Tasks   (glyphs: [ ] open  [~] in-progress  [!] blocked  [x] done)

## Active

- [ ] di-registration — `BlobManager` is `internal` and `BlobManagerModule.cs` is commented out, so
      nothing outside the assembly can consume the library
- [ ] content-hash — `Hash` is an unchecked claim: compute it during the write, verify a declared one,
      and decide on a multi-step write session (hasher state cannot be persisted between calls)
- [~] range-read — seekable-read promise done; explicit range overload deferred, reasoning recorded
- [ ] integrity-audit — nothing checks that stored bytes still match the recorded `Size`/`Hash` after an
      out-of-band edit, and orphaned content is undetectable because the store cannot be enumerated
- [ ] url-safe-key-separator — `/` in a key means directories, but the key is the public ID and a slash
      breaks URL routes; distinct keys can also collide after filename sanitisation
- [ ] read-lock-persists-mutations — the read path persists the whole record, so a mutation made under
      a read lock reaches storage and concurrent readers can race
- [ ] batch-content-delete — bulk deletion deletes content one key at a time

## Done (recent)

- [x] delete-blob-content — all deletion paths now remove the stored bytes too (2026-08-05)
