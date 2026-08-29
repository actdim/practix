---
protocol: along
slug: refactor-script-engine-params
type: feat
status: done
priority: high
created: 2026-08-17
updated: 2026-08-17
completed: 2026-08-17
agent: antigravity
tags: []
milestone: v1.3.0-knowledge-base-and-graph
blocked_by: []
related: []
---

# Feature: Standardize ScriptEngine and Interpolator on Collision-Free @params Variable

## Goal
Use `@params` as the default script parameter variable name across `ScriptEngine` and `Interpolator`.

## Accomplished
- Standardized `ScriptEngine.DefaultInputParameterName = "@params"`.
- Since `params` is a reserved C# keyword, user local variables cannot collide with `@params`.
- Retained customizable `inputParameterName` parameter for overriding when needed.
- Passed 100% of unit tests.
