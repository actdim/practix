---
protocol: along
protocol_version: "2.2.26"
slug: feat--expose-blobmanager-and-corejsonserializer
type: feat
status: done
priority: medium
created: 2026-09-24
updated: 2026-09-24
completed: 2026-09-24
agent: antigravity
tags: [api, visibility, bytepath, json, public-api]
milestone: v2.0.0-along-transition
blocked_by: []
---

# Expose BlobManager, BlobManagerBuilder, and CoreJsonSerializer as Public

## Overview
Expose key engine implementations as public types to enable direct consumption without forced DI dependency injection:
1. `ActDim.BytePath.BlobManager`: change from `internal class` to `public class`.
2. `ActDim.BytePath.Extensions.BlobManagerBuilder`: change from `internal sealed class` to `public sealed class`.
3. `ActDim.Practix.Json.CoreJsonSerializer`: change from `internal class` to `public class`.

## Requirements
- Verify that exposing constructors and member signatures does not violate encapsulation or leak internal contracts.
- Ensure XML doc comments are authoritative or inheritdoc where appropriate.
- Verify solution builds and passes full test suite without warnings or regressions.
