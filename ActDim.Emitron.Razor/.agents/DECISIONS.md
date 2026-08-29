# Decisions

Append-only record of non-trivial architectural decisions.

## 2026-08-26: #1: Dedicated Razor Engine Module
Created `ActDim.Emitron.Razor` as a separate assembly referencing `ActDim.Emitron` to keep `ActDim.Emitron` core lightweight while providing Razor syntax template compilation.

