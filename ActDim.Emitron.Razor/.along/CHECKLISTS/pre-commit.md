---
protocol: along
slug: pre-commit
title: Pre-Commit Quality Gate
category: pre-commit
created: 2026-08-29
updated: 2026-08-29
---

# Pre-Commit Verification Checklist

1. [ ] Code compiles and unit tests pass.
2. [ ] Mandatory git diff inspected for zero unintended deletions.
3. [ ] No API keys, secrets, or sensitive credentials committed.
4. [ ] Filenames are Windows-safe (no colons, YYYY-MM-DD dates).
