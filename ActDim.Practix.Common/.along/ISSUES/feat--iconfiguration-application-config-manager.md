---
protocol: along
slug: feat--iconfiguration-application-config-manager
type: feat
status: open
priority: medium
created: 2026-08-17
updated: 2026-08-17
agent: antigravity
tags: []
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Convenient Application Configuration Manager based on IConfiguration

## Context
The legacy `IJsonConfigurationManager` / `JsonConfigurationManager` was a custom JSON file loading/saving utility. Modern .NET applications use `Microsoft.Extensions.Configuration.IConfiguration` with options binding (`IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>`) and Environment / AppSettings / UserSecrets providers.

## Objectives
- Design and implement a convenient wrapper/manager class around `IConfiguration` for application settings access and strongly-typed section binding.
- Support typed setting extraction (`Get<T>()`), dynamic section retrieval, and validation capabilities.
- Add DI registration extensions in `ActDim.Practix.Common` (`AddConfigurationManager()`).
- Provide unit test coverage verifying section binding, fallback defaults, and environment override behavior.
