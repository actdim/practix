---
protocol: along
protocol_version: "2.2.8"
slug: setup-and-workflow
title: Setup & Workflow
type: setup-workflow
created: 2026-08-31
updated: 2026-09-02
tags: [workflow, setup]
---

# Setup & Workflow

Build instructions, test suites, local development servers, and deployment.

- **Docs discipline**: XML docs say what a member does for the caller. Rationale goes to `DECISIONS.md`, requirements on implementations to `AGENTS.md`, and `README.md` is the only place that deliberately explains why the API is shaped as it is.
- **Before touching `IBlobDataStore`**: read the object-store capability table in `VISION.md`. The contract is deliberately kept expressible on S3/Azure, not just on a file system.
- **Sibling copy**: `CanarySystems.FileStorage` carries the same design over a different query layer and has received these changes. Its `BlobManager` ctor takes a logger provider, its data store has a flat path layout and an `IFileStorageConfiguration` ctor, and its registry timeout is hard-wired to 5 minutes with no overload: preserve those when syncing.
- **Open tasks**: `content-hash`, `integrity-audit`, \
ead-lock-persists-mutations\, \atch-content-delete\, \
ange-read\'s deferred second half, \dd-try-create-with-conflict-behavior\ (one-shot `CreateAsync` extension methods), and `multipart-upload-session` for resumable out-of-order uploads.
