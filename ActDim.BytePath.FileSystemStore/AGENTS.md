<!-- BEGIN ALONG-PROTOCOL ref=../AGENTS.md (managed by along-init - do not edit by hand) -->
This folder belongs to a repository that uses the ALONG structure. The full working
guidance + agent-context protocol live once in the nearest ancestor `AGENTS.md` (`../AGENTS.md`) -
read it there. This folder keeps its OWN `.along/` state; use the nearest one.
Only this folder's specifics follow.
<!-- END ALONG-PROTOCOL -->## Project specifics

<!-- BEGIN ALONG-RULES -->
See the following engineering guidelines:
- `[languages/csharp.md](file://.along/rules/languages/csharp.md)`
<!-- END ALONG-RULES -->

Sharded physical file system data store (`FileSystemBlobDataStore`), key prefix routing, SHA-256 integrity hashing, stream pumping, and DI extensions (`WithFileSystemDataStore()`, `AddFileSystemBlobDataStore()`).

