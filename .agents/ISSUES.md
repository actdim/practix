# Active Issues

_Solution-level issues board. Project-specific issues live in their respective subproject folders (e.g. `ActDim.Practix.Common/.agents/ISSUES/`, `ActDim.Observability/.agents/ISSUES/`, `ActDim.BlobManager/.agents/ISSUES/`, etc.)._

## Active

## Subproject Issue Boards
- `ActDim.Practix.Common`: [`.agents/ISSUES.md`](file:///d:/Src/my/actdim/public/dotnet/ActDim.Practix.Common/.agents/ISSUES.md)
  - `feat--dynamic-array-json-converter`
  - `debt--arraysegment-blockcopy-optimization`
  - `debt--enumerable-dead-code`
  - `debt--factorydict-replace-rwlock`
  - `debt--stringsplit-regex-cache`
  - `feat--encoding-async-extensions`
  - `feat--enumerable-estimation-extensions`
  - `feat--iconfiguration-application-config-manager`
  - `feat--large-payload-compression`

## Done (recent)
- `debt--remove-autofac-dependency`: Remove Autofac dependency and migrate to standard Microsoft Dependency Injection
- `feat--extract-practix-json-assembly`: Extract JSON serialization subsystem into dedicated ActDim.Practix.Json assembly
- `debt--json-serializer-reflectron-optimization`: Replace un-cached reflection in StandardJsonSerializer with fast compiled expression tree setters and property metadata cache
- `feat--emitron-tests`: Create ActDim.Emitron unit test coverage for Roslyn compilation and evaluation
