# Context

Current state snapshot of `ActDim.BytePath.FileSystemStore`.

## Overview
- Physical file system blob data store (`FileSystemBlobDataStore`).
- KeyPrefix routing, directory sharding, stream pumping, and DI extension methods (`WithFileSystemDataStore()`, `AddFileSystemBlobDataStore()`).
- `DeleteAsync` gracefully handles missing files and missing target directories (`FileNotFoundException`, `DirectoryNotFoundException`), returning `false` per `IBlobDataStore` contract. Empty directory pruning guarded against concurrency race conditions.
