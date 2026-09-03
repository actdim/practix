---
protocol: along
protocol_version: "2.2.18"
slug: compression-and-archives
title: Stream & Payload Compression and Archiving
type: topic
created: 2026-09-03
updated: 2026-09-03
tags: [compression, archiving, gzip, brotli, tar, zip, zero-allocation]
---

# Stream & Payload Compression and Archiving

`CompressionManager` is a high-performance compression and archiving facade built exclusively on the .NET 10 Base Class Library (BCL), supporting GZip, Deflate, Brotli, ZIP, and TAR formats without third-party native codec dependencies.

---

## Zero-Allocation Design Principles

`CompressionManager` enforces strict memory and allocation rules across all entry points:

1. **No Managed Heap Thrashing**: Temporary scratch streams come directly from `MemoryManager.Default` (a pooled `RecyclableMemoryStreamManager`).
2. **ArrayPool Buffer Slices**: Copies and buffer reading rent blocks from `ArrayPool<byte>.Shared` (default 80 KB bucket hint, below LOH threshold).
3. **No Redundant Stream Buffering**: Does not wrap codecs in `BufferedStream`: `GZipStream`, `DeflateStream`, and `BrotliStream` buffer internally; extra buffering would only add heap allocations and extra copy passes.
4. **Stack-Allocated Format Sniffing**: Header sniffing uses `stackalloc byte[SignatureBufferLength]` and `ReadOnlySpan<byte>` literals.
5. **Stream Ownership Conventions**:
   - Streams created by the manager are returned rewound to position `0` and owned by the caller (must be disposed).
   - Destination streams supplied by the caller are written from their current position and left open for composition.

---

## Supported Codecs & Formats

| Category | Format | Detection | Codec Support |
| :--- | :--- | :---: | :--- |
| **Stream Compression** | `CompressionFormat.GZip` | Auto (0x1F 0x8B) | Full BCL `GZipStream` |
| | `CompressionFormat.Deflate` | Explicit | Full BCL `DeflateStream` (RFC 1951) |
| | `CompressionFormat.Brotli` | Explicit | Full BCL `BrotliStream` |
| | `BZip2`, `LZMA`, `7z` | Sniffed | Throws `NotSupportedException` (No BCL Codec) |
| **Archive Containers** | `ArchiveFormat.Zip` | Auto (`PK..`) | Full BCL `ZipArchive` (Random Access) |
| | `ArchiveFormat.Tar` | Auto (`ustar` magic) | Full BCL `TarReader` / `TarWriter` (Streaming) |

> [!NOTE]
> Brotli and raw Deflate are headerless by design. They cannot be auto-detected from magic bytes and must be decompressed with an explicit `CompressionFormat` parameter.

---

## Stream & Buffer Compression APIs

```csharp
ICompressionManager manager = new CompressionManager();

// Compress in-memory payload to pooled stream
await using (Stream compressedStream = await manager.CompressAsync(rawBytes, CompressionFormat.Brotli))
{
    // Stream is ready to transmit / persist
}

// Zero-allocation stream-to-stream pipeline
await manager.CompressAsync(sourceStream, destinationStream, CompressionFormat.GZip);

// Decompress with automatic format detection
await using (Stream decompressedStream = await manager.DecompressAsync(compressedData))
{
    // Decompressed payload
}

// Pooled buffer owner return (zero heap array allocation)
using (IBufferOwner<byte> bufferOwner = await manager.DecompressToBytesAsync(compressedData))
{
    ReadOnlyMemory<byte> memory = bufferOwner.Memory;
}
```

---

## Archive Inspection & Extraction (`DecompressArchiveAsync`)

Archives are processed via asynchronous streaming callbacks:

```csharp
await manager.DecompressArchiveAsync(archiveStream, async (entry, openRead) =>
{
    Console.WriteLine($"Entry: {entry.FullName}, Size: {entry.Size}, Type: {entry.EntryType}");

    if (entry.EntryType == ArchiveEntryType.RegularFile)
    {
        await using Stream entryStream = await openRead();
        // Consume entryStream before callback returns
        await ProcessEntryFileAsync(entryStream);
    }

    return true; // Return false to stop traversal
});
```

---

## Key Invariants

1. **Callback Stream Lifetime**: The `openRead` stream handed to archive reader delegates is owned by `CompressionManager` and disposed as soon as the delegate completes. Callers must fully consume or pipe the stream before returning.
2. **Seekability Requirement for Detection**: Auto-detecting compression/archive format on a stream requires seekability (`stream.CanSeek == true`) so magic bytes can be inspected and rewound. For non-seekable streams, either pass the format explicitly or buffer via `stream.ToMemoryAsync()`.

