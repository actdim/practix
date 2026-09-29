---
protocol: along
protocol_version: "4.0.0"
slug: task--github-actions-ci-workflow
type: task
status: done
completed: 2026-09-29
priority: normal
created: 2026-09-29
updated: 2026-09-29
agent: antigravity
tags: [ci, github-actions, test-automation]
blocked_by: []
milestone: v1.3.0-knowledge-base-and-graph
related: []
---

# Configure GitHub Actions CI Workflow for .NET 10 Test Suite

## Context
Add automated continuous integration workflow for GitHub Actions to build `ActDim.Practix.sln` and run the xUnit test suite on pull requests and pushes to `main` and `master`.

## Acceptance Criteria
- [x] Create `.github/workflows/ci.yml` targeting .NET 10 preview SDK on `ubuntu-latest`.
- [x] Configure steps: checkout, setup-dotnet (10.0.x with preview quality), restore, build, and test.
- [x] Verify test suite passes locally.
- [x] Reconcile Along protocol entities (issue, session log, history).
