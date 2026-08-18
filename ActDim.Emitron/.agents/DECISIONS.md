# Architecture Decisions

## 2026-08-17 — #1: Standardize Default Parameter Name to @params
- **Status**: Accepted
- **Context**: Need a default parameter variable name in Roslyn scripts that is 100% collision-free with local C# script variables.
- **Decision**: Use `@params` as default parameter variable name. Because `params` is a reserved C# keyword, users cannot declare local `var params = ...` variables, eliminating collision risks.

## 2026-08-18 — #2: Support Assemblies and Usings via Script Directives and EmitronOptions
- **Status**: Accepted
- **Context**: Scripts and host applications need flexible mechanisms to reference assemblies and import namespaces both inline in script source and programmatically via options.
- **Decision**: Standardize on native C# nomenclature: `Assemblies` (and `AssemblyReferences`) for assembly references, and `Usings` for namespace imports. Support `#r "AssemblyName"` and `using Namespace;` directives with auto-injection of parameters after header directives, and provide `EmitronOptions` with `SearchPaths`.

