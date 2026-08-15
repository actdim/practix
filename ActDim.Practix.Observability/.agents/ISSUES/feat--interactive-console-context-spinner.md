---
slug: interactive-console-context-spinner
type: feat
status: open
priority: medium
created: 2026-08-14
updated: 2026-08-14
---

# Feature: Interactive Console Context Animation & Progress Spinner

## Description
Create a dedicated project/package (`ActDim.Practix.Observability.Console` or `ActDim.Practix.ConsoleUI`) that consumes `CallContext` ambient properties (`Status`, `Progress`, `Icon`, `Tags`) and provides a rich interactive console UI.

## Key Requirements
1. **Braille Spinner & Progress Animation:**
   - Render smooth Braille character animations (dot spinners e.g. `⠋ ⠙ ⠹ ⠸ ⠼ ⠴ ⠦ ⠧ ⠇ ⠏`).
   - Dynamic progress percentage bar for `ObservabilityContextPropertyNames.Progress`.
2. **Icon & Status Visualization:**
   - Display active emoji/icon (`ObservabilityContextPropertyNames.Icon`) alongside status text (`ObservabilityContextPropertyNames.Status`).
3. **ANSI & Terminal Fallbacks:**
   - Detect terminal capability for ANSI color codes and UTF-8 Braille rendering with clean fallbacks.
