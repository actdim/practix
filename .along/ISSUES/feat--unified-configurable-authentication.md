---
protocol: along
protocol_version: "2.2.26"
slug: feat--unified-configurable-authentication
type: feat
status: open
priority: high
created: 2026-09-09
updated: 2026-09-09
agent: antigravity
tags: [auth, security, oidc, zitadel, jwt, apikey, cookie, basic, configuration]
related: [feat--service-configurable-auth-schemes, feat--appregistry-zitadel-and-auth]
---

# Unified Configurable Authentication Subsystem

## Overview
Implement end-to-end support for all authentication schemes defined in `AuthSchemeType` via configuration (`AppSettings.AuthSchemes`) across `ActDim.Practix.Service` and `AppRegistry`.
OpenID Connect (OIDC) will be backed by Zitadel as external identity provider.

## Child Subproject Issues
- `[ActDim.Practix.Service:feat--service-configurable-auth-schemes]`
- `[AppRegistry:feat--appregistry-zitadel-and-auth]`

## Target Authentication Schemes
1. `Oidc` (Zitadel): JWT Bearer validation with Zitadel discovery / JWKS, role mapping (`urn:zitadel:iam:org:project:roles`), and optional OAuth 2.0 token introspection.
2. `LocalJwt`: Symmetric/asymmetric token issuance and local validation.
3. `ApiKey`: Header/query-based API key validation with configured key hashes and client mapping.
4. `Cookie`: Stateful session cookie authentication for web clients.
5. `Basic`: HTTP Basic authentication for legacy/system integrations.
6. `None`: Explicit anonymous access.

## Architecture & Implementation Plan
- Service layer (`ActDim.Practix.Service`): Config-driven authentication builder, scheme registration, dynamic scheme selection, and Swagger/OpenAPI security scheme enrichment.
- Registry layer (`AppRegistry`): Decouple user resolution from local GUID `sub` to support Zitadel string subjects and email lookups, handle Zitadel role claims, and configure OIDC validation without local secret keys.

