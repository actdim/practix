# ActDim.BytePath.FileSystemStore

File-system blob data store implementation for `ActDim.BytePath`.

## Features
- **URL-Safe Hierarchy & Hash-Sharded Storage**: Splits multi-segment keys by `HierarchySeparator` (defaults to `':'` per RFC 3986 `pchar`) into directory structures, or applies uniform 2-level `XxHash3` hash-sharding for flat keys.
- **Lossless Bijective Escaping**: Reversibly escapes invalid filename characters (`%XX`) while preserving file extensions for `ResolveLocationAsync`.
- **Windows Device Name & Trailing Char Protection**: Escapes DOS device names (`CON`, `PRN`, `AUX`, `NUL`, `COM1`-`COM9`, `LPT1`-`LPT9`) and trailing dots/spaces to prevent silent Win32 file aliasing and collisions.
- **Direct & Piped Streaming**: Zero-copy stream reads and file writes.
- **Empty Directory Pruning**: Automatically cleans up empty parent subdirectories on deletion down to the root base directory.

## Installation

```bash
dotnet add package ActDim.BytePath.FileSystemStore
```

## Key Format & Storage Layout

### Logical Separator (`:`)
Keys use `:` (colon) as the standard logical namespace delimiter (e.g. `tenant:reports:2026:august.pdf`). Colon is unreserved in RFC 3986 `pchar`, making keys safe to pass directly inside URL path segments (`/api/blobs/{key}`) without percent-encoding issues or catch-all route collisons.

### Storage Layout Rules
- **Multi-Segment Keys:** When `HierarchySeparator` is set (default `':'`), a key with multiple segments is placed in matching subdirectories (`_basePath/tenant/reports/2026/august.pdf`).
- **Single-Segment / Flat Keys:** Keys without the hierarchy separator are uniformly distributed into 2-level directory buckets derived from the 64-bit non-cryptographic `XxHash3` of the key (`_basePath/hash[0..2]/hash[2..4]/filename`).
- **Pure Hash-Sharding:** Setting `options.HierarchySeparator = null` disables directory splitting and routes all keys through hash-sharded buckets.

### Escaping Mechanics
1. **Reversible `%XX` Encoding:** Invalid filesystem characters (`Path.GetInvalidFileNameChars()`) and `%` itself are hex-encoded (`%XX`), guaranteeing zero collisions.
2. **File Extension Preservation:** Standard filename characters and extensions (`.png`, `.pdf`) remain untouched for direct path resolution via `ResolveLocationAsync`.
3. **DOS/Windows Device Names:** Reserved names (`CON`, `PRN`, `AUX`, `NUL`, `COM1`-`COM9`, `LPT1`-`LPT9` with or without extensions) have their first character escaped (e.g. `con.txt` -> `%63on.txt`) so Win32 never mistakes a file for a system device pipe.
4. **Trailing Dots & Spaces:** Win32 silently trims trailing `.` and ` `; escaping them (`%2E`, `%20`) ensures `name.` and `name` never alias to the same file.

## Quick Start

```csharp
services.AddBlobManager(builder =>
{
    builder.WithFileSystemDataStore(options =>
    {
        options.BaseDirectory = "./my-blobs";
        options.HierarchySeparator = ':'; // default, or null for pure hash-sharding
    });
});
```

