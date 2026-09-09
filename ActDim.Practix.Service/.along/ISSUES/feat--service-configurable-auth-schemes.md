---
protocol: along
protocol_version: "2.2.26"
slug: feat--service-configurable-auth-schemes
type: feat
status: open
priority: high
created: 2026-09-09
updated: 2026-09-09
agent: antigravity
tags: [auth, security, oidc, zitadel, jwt, apikey, cookie, basic, aspnetcore]
parent: feat--unified-configurable-authentication
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Support Configurable Authentication Schemes in ActDim.Practix.Service

## Problem
In `ActDim.Practix.Service/CoreService.cs` (lines 420-508), only `LocalJwt` is currently registered via `AddJwtBearer`.
1. `Oidc` is marked with `// TODO: support other types of authentication schemes (OIDC, etc.)`.
2. `ApiKey`, `Cookie`, `Basic`, and `None` from `AuthSchemeType` have no configuration models or authentication handlers.
3. `AuthConfig.SchemeType` is ignored; the scheme is inferred implicitly via `if (authConfig.LocalJwt != default)`.
4. There is no dynamic policy scheme selector or forwarder for multiple concurrent schemes (e.g., Bearer token vs API Key).

## Requirements
1. DTO models:
   - Complete `ApiKeyAuthConfig` in `Settings/AuthConfig.cs`.
   - Add `CookieAuthConfig` and `BasicAuthConfig` in `Settings/`.
2. CoreService configuration logic:
   - Evaluate `authConfig.SchemeType` explicitly.
   - Register OIDC JwtBearer with Zitadel authority: `options.Authority = oidc.Authority`, `options.Audience = oidc.DefaultAudience`, automatic JWKS retrieval from `.well-known/openid-configuration`.
   - Register custom `ApiKeyAuthenticationHandler` for `ApiKey`.
   - Register `CookieAuthenticationOptions` for `Cookie`.
   - Register `BasicAuthenticationHandler` for `Basic`.
   - Configure multi-scheme forwarding (e.g. `AddPolicyScheme` or dynamic forward default scheme).
3. Swagger / OpenAPI integration:
   - Configure security definitions (Bearer JWT, ApiKey, Basic) in `Extensions/OpenApiServiceCollectionExtensions.cs` based on enabled schemes.

