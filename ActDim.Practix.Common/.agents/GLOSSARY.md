# Glossary

_Domain terms. Add a term when you introduce or clarify it._

<!-- - **Term**: definition. -->

- **Compression format** (`CompressionFormat`): a codec applied to a single byte stream, with no file
  metadata or structure: GZip, Brotli, Deflate, … Contrast with *archive format*.
- **Archive format** (`ArchiveFormat`): a container holding multiple named entries: ZIP, TAR, 7z, RAR. The
  container and the codec are independent concerns (a `.tar.gz` is a TAR archive inside a GZip stream).
- **Archive entry** (`IArchiveEntry` / `ArchiveEntry`): one named item inside an archive: uncompressed size,
  `ArchiveEntryType`, last-write time, compressed size, link target, and a back-reference to the shared
  `IArchiveInfo`. Metadata only; the entry *data* is reached through the `OpenStreamDelegate` handed to a
  reader/writer callback and is valid only for the duration of that callback.
- **Entry type** (`ArchiveEntryType`): what an entry represents: `RegularFile` / `Directory` / `SymbolicLink` /
  `HardLink` / `Other`. Only a regular file has a data section, so this: not a zero `Size`: is what
  distinguishes a directory from an empty file.
- **Buffer owner** (`IBufferOwner<T>`): a disposable handle over a rented buffer that carries the valid
  `Length` (the backing array may be larger). Disposing returns the buffer to the pool; the caller of any
  API returning one MUST dispose it.
- **Temp / scratch stream**: a pooled `RecyclableMemoryStream` from `MemoryManager.Default`, used wherever a
  payload must be materialized to be re-read or handed back. Never `ToArray()` it (the manager is configured
  with `ThrowExceptionOnToArray`).
