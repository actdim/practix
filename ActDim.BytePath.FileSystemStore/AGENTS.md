<!-- BEGIN ACTDIM-AGENTS-PROTOCOL ref=../AGENTS.md (managed by init-agents: do not edit by hand) -->
This folder belongs to a repository that uses the ACTDIM-AGENTS structure. The full working
guidance + agent-context protocol live once in the nearest ancestor `AGENTS.md` (`../AGENTS.md`) -
read it there. This folder keeps its OWN `.agents/` state; use the nearest one.
Only this folder's specifics follow.
<!-- END ACTDIM-AGENTS-PROTOCOL -->

## Project specifics

Sharded physical file system data store (`FileSystemBlobDataStore`), key prefix routing, SHA-256 integrity hashing, stream pumping, and DI extensions (`WithFileSystemDataStore()`, `AddFileSystemBlobDataStore()`).

