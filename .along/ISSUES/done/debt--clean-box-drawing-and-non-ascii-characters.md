---
protocol: along
protocol_version: "2.2.26"
slug: debt--clean-box-drawing-and-non-ascii-characters
type: debt
status: done
priority: low
created: 2026-09-24
updated: 2026-09-24
completed: 2026-09-24
agent: antigravity
tags: [typography, cleanup, ascii, comments]
milestone: v2.0.0-along-transition
blocked_by: []
related: []
---

# Clean Box-Drawing and Non-ASCII Characters in Source Files

## Overview
Scan the entire repository for non-ASCII box-drawing characters (U+2550, U+2500, etc.) and replace them with standard ASCII equivalents (`=`, `-`) in comments and headers.

## Requirements
- Scan all `.cs`, `.md`, and project files.
- Replace non-ASCII box/line drawing characters with standard ASCII equivalents (`=`, `-`).
- Ensure all tests pass with 0 failures and 0 regressions.
