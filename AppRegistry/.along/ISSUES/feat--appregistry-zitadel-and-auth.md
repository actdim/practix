---
protocol: along
protocol_version: "2.2.26"
slug: feat--appregistry-zitadel-and-auth
type: feat
status: open
priority: high
created: 2026-09-09
updated: 2026-09-09
agent: antigravity
tags: [appregistry, zitadel, oidc, auth, identity, jwt, security]
parent: feat--unified-configurable-authentication
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Support Zitadel OIDC and Multi-Auth in AppRegistry

## Problem
In `AppRegistry.Service/AppContext.cs`:
1. `SetIdentityAsync` parses `Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub))`. Zitadel subjects are strings, not local GUIDs, causing an immediate `FormatException`.
2. User lookup relies solely on local `GetByIdAsync(Guid)`. Zitadel users must be resolved via email, external subject mapping, or auto-provisioned (JIT).
3. Zitadel role claims (`urn:zitadel:iam:org:project:roles` or project-specific role arrays) are not mapped to application roles.
4. `GetAccessTokenAsync` and `ValidateAccessTokenAsync` throw `NotSupportedException` for anything other than `LocalJwt`.
5. Under Zitadel OIDC, tokens are issued by Zitadel, not created locally with a symmetric key.

## Requirements
1. Update `AppContext.SetIdentityAsync`:
   - Safely extract `sub` without hardcoded `Guid.Parse`.
   - Support lookup by `Email` or `ExternalSubjectId`.
   - Map Zitadel role claims to `UserInfo` and claims identity.
2. Update token validation:
   - For Zitadel OIDC, validate tokens using Zitadel JWKS / discovery metadata or Zitadel token introspection endpoint.
3. Update `User` domain model:
   - Support external subject mapping (`ExternalId` / `ZitadelUserId`) or extend metadata to store provider claims.

