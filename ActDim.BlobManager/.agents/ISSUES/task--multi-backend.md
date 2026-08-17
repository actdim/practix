# multi-backend — multiple stores, each self-describing

## Problem

`IBlobManager` holds exactly one `IBlobDataStore`. You can register either FileSystem or S3,
but not both. When an app needs both (e.g. files for user uploads, S3 for cache), there is no
way to express that.

## Decision

Each `IBlobManager` registers normally in DI. Each knows its own key prefix via `KeyPrefix`.
Clients iterate all manifests, call `ResolveKey(key)`, and pick the first match.

```csharp
// Registration — each is a normal DI registration:
services.AddBlobManager("files", "fs:", opts => opts.UseFileSystem(@"D:\data"));
services.AddBlobManager("s3",   "s3:", opts => opts.UseS3(s3Config));

// Usage — client picks:
var manifests = provider.GetServices<BlobManagerManifest>();
foreach (var m in manifests)
    if (m.ResolveKey(key))
    {
        await m.Manager.WriteAsync(key, stream, ct);
        break;
    }
```

## Key format

Keys carry a prefix: `fs:my-blob`, `s3:my-blob`. The prefix is the store's `KeyPrefix`.
Empty prefix = catch-all (matches any key).

## Implementation

### New members

```csharp
// IBlobDataStore — new property
string KeyPrefix { get; }

// New record
public class BlobManagerManifest
{
    public string Name { get; }
    public string KeyPrefix { get; }
    public IBlobDataStore DataStore { get; }
    public IBlobManager Manager { get; }
    public bool ResolveKey(string key) => key.StartsWith(KeyPrefix);
}

// IBlobManager — new method
BlobManagerManifest Manifest { get; }

// New extensions
public static class BlobManagerServiceCollectionExtensions
{
    public static IServiceCollection AddBlobManager(
        this IServiceCollection, string name, string keyPrefix,
        Action<BlobManagerOptions> configure);

    public static IServiceCollection AddBlobManagerFileSystem(
        this IServiceCollection, string name, string basePath,
        string keyPrefix = "fs:");

    public static IServiceCollection AddBlobManagerS3(
        this IServiceCollection, string name, Action<S3Options> configure,
        string keyPrefix = "s3:");
}
```

### Contract rules

1. `IBlobDataStore.KeyPrefix` — the prefix this store handles. Empty = catch-all.
2. `BlobManagerManifest.ResolveKey` — delegates to `KeyPrefix`.
3. `IBlobManager.Manifest` — returns the manifest for this manager.
4. No routing inside `BlobManager` — the client decides which manager to use.
5. No keyed services — each registration is independent.

## Open questions

- Should `ResolveKey` be on `IBlobManager` or only on `Manifest`?
  - On `Manifest` is cleaner — it's a descriptor, not behavior.
- What about catch-all stores? If multiple have empty prefix, which wins?
  - First match wins. Document that catch-all should be last.
- Should we add a typed helper `IBlobManagerResolver` that wraps the iteration?
  - Maybe later. For now, the loop is simple enough.
