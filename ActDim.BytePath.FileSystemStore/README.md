# ActDim.BytePath.FileSystemStore

File-system blob data store implementation for `ActDim.BytePath`.

## Features
- **Hierarchical Hash-Sharded Directories**: Automatically calculates 2-level directory prefixes from 64-bit non-cryptographic hashes (`XxHash3`) or escapes hierarchical path keys.
- **Direct & Piped Streaming**: Zero-copy stream reads and file writes.
- **Empty Directory Pruning**: Cleans up parent subdirectories on deletion down to the root base directory.

## Installation

```bash
dotnet add package ActDim.BytePath.FileSystemStore
```

## Quick Start

```csharp
services.AddBlobManager(builder =>
{
    builder.WithFileSystemDataStore(options =>
    {
        options.BaseDirectory = "./my-blobs";
    });
});
```
