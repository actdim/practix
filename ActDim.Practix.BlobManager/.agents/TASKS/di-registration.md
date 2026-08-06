# di-registration

- status: open
- created: 2026-08-06
- updated: 2026-08-06

## Problem

Nothing outside the assembly can use this library. `BlobManager` is `internal`, there is no factory
and no container registration; `BlobManagerModule.cs` exists but every line of it is commented out.
Tests construct `new BlobManager(dataStore, registry)` directly, which works only because of
`InternalsVisibleTo`.

This is the first item on the roadmap in `VISION.md` — it blocks consumption regardless of which
backend is used, so it outranks every other open task.

## Design

Open questions to settle before writing code, since they decide the shape:

- **Container or plain factory?** The sibling `CanarySystems.FileStorage` registers through an Autofac
  module, and the commented-out `BlobManagerModule.cs` sketches the same. A plain
  `IServiceCollection` extension plus a container-agnostic factory keeps this library from depending
  on a container at all — preferable unless the consuming projects are all Autofac.
- **Does `BlobManager` become public, or stay internal behind a factory?** Staying internal keeps the
  surface honest: consumers get `IBlobManager` and cannot depend on the implementation. It also keeps
  the constructor free to change.
- **Who owns the SQLite connection and the base path?** `SQLiteBlobRegistry` takes a connection string
  and a default timeout; `FileSystemBlobDataStore` takes a base path. An options object registered in
  the container is the obvious route, but note the registry constructor runs `EnsureSchemaAsync`
  synchronously — so resolution does I/O, which matters for container validation and for startup
  ordering.
- **Lifetime.** The registry holds a `SQLiteAsyncConnection` and a `SemaphoreSlim` that serializes all
  DB access, so it must be a singleton; two instances over the same file would each serialize only
  their own traffic and the locking guarantees would not hold across them. This has to be stated in the
  registration, not left to the consumer.

## Done when

- [ ] a consumer outside the assembly can obtain an `IBlobManager` without `InternalsVisibleTo`
- [ ] registry registered as a singleton, with the reason documented at the registration site
- [ ] `BlobManagerModule.cs` either implemented or deleted — not left commented out
- [ ] `README.md`'s status note removed, and its quick-start example made real
