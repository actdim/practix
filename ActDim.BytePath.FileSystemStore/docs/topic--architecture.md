---
protocol: along
protocol_version: "2.2.8"
slug: architecture
title: System Architecture & Flow
type: architecture
created: 2026-08-31
updated: 2026-09-02
tags: [architecture]
---

# System Architecture & Flow

High-level architectural components, module boundaries, and execution models.

Physical file system blob data store (FileSystemBlobDataStore). KeyPrefix routing, directory sharding, stream pumping, and DI extension methods (WithFileSystemDataStore(), AddFileSystemBlobDataStore()). DeleteAsync gracefully handles missing files and missing target directories (FileNotFoundException, DirectoryNotFoundException), returning alse per IBlobDataStore contract. Empty directory pruning guarded against concurrency race conditions.
