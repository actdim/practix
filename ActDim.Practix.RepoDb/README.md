# ActDim.Practix.RepoDb

`ActDim.Practix.RepoDb` provides RepoDb extensions, lambda expression SQL translators, and best practices for ActDim.Practix.

## Features

- **Lambda Expression Query Translator:** Translate C# lambda expressions (`Expression<Func<T, bool>>`, `Expression<Func<T, object>>`) into SQL statements and parameters, respecting RepoDb `FluentMapper` and property/column aliases.
- **Fluent Helper Extensions:** High-productivity helpers built on top of RepoDb.

## Installation

```bash
dotnet add package ActDim.Practix.RepoDb
```

## License

This project is licensed under the [MIT License](LICENSE).
