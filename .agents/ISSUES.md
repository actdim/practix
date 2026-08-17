# Active Issues

_Solution-level issues board. Project-specific issues live in their respective subproject folders (e.g. `ActDim.Emitron/.agents/ISSUES/`, `ActDim.Practix.Common/.agents/ISSUES/`, `ActDim.Observability/.agents/ISSUES/`, `ActDim.BlobManager/.agents/ISSUES/`, etc.)._

## Active

## Subproject Issue Boards
- `ActDim.BytePath`: [`.agents/ISSUES.md`](file:///d:/Src/my/actdim/public/dotnet/ActDim.BytePath/.agents/ISSUES.md)
  - `task--multi-backend`
  - `task--di-registration`
  - `task--delete-blob-content`
- `ActDim.Emitron`: [`.agents/ISSUES.md`](file:///d:/Src/my/actdim/public/dotnet/ActDim.Emitron/.agents/ISSUES.md)
  - `debt--rename-script-evaluator-to-script-engine`
  - `feat--refactor-script-engine-params`
  - `feat--emitron-tests`
- `ActDim.Practix.Common`: [`.agents/ISSUES.md`](file:///d:/Src/my/actdim/public/dotnet/ActDim.Practix.Common/.agents/ISSUES.md)
  - `feat--dynamic-array-json-converter`
  - `debt--arraysegment-blockcopy-optimization`
  - `debt--enumerable-dead-code`
  - `debt--factorydict-replace-rwlock`
  - `debt--stringsplit-regex-cache`
  - `feat--encoding-async-extensions`
  - `feat--enumerable-estimation-extensions`
  - `task--configure-nuget-packaging`
  - `feat--large-payload-compression`

## Done (recent)
- `task--multi-backend`: Multiple IBlobDataStore instances with KeyPrefix routing and DI support in ActDim.BytePath
- `task--nuget-package-readmes`: Create and configure required NuGet README.md files across 8 packable projects
- `debt--rename-script-evaluator-to-script-engine`: Rename ScriptEvaluator to ScriptEngine in ActDim.Emitron
- `feat--refactor-script-engine-params`: Standardize ScriptEngine and Interpolator on collision-free @params parameter variable
- `task--configure-nuget-packaging`: Configure 6 ActDim projects for NuGet package publishing
- `debt--remove-autofac-dependency`: Remove Autofac dependency and migrate to standard Microsoft Dependency Injection
- `feat--extract-practix-json-assembly`: Extract JSON serialization subsystem into dedicated ActDim.Practix.Json assembly
- `debt--json-serializer-reflectron-optimization`: Replace un-cached reflection in StandardJsonSerializer with fast compiled expression tree setters and property metadata cache
- `feat--emitron-tests`: Create ActDim.Emitron unit test coverage for Roslyn compilation and evaluation
