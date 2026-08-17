# Architecture Decisions

## 2026-08-17 — #1: Standardize Default Parameter Name to @params
- **Status**: Accepted
- **Context**: Need a default parameter variable name in Roslyn scripts that is 100% collision-free with local C# script variables.
- **Decision**: Use `@params` as default parameter variable name. Because `params` is a reserved C# keyword, users cannot declare local `var params = ...` variables, eliminating collision risks.
