---
protocol: along
slug: clean-box-drawing-and-non-ascii-characters
date: 2026-09-24
agent: antigravity
summary: "Clean Box-Drawing and Non-ASCII Characters in Source Files"
---

# Session: Clean Box-Drawing and Non-ASCII Characters in Source Files
Date: 2026-09-24
Issue: `debt--clean-box-drawing-and-non-ascii-characters`

## Objectives
Scan the repository for box-drawing characters (U+2550, U+2500, etc.) and non-ASCII punctuation in comments/documentation, replacing them with standard ASCII equivalents:
1. Replaced box-drawing horizontal lines with ASCII hyphens (`-`) and double-lines with ASCII equals signs (`=`).
2. Replaced unicode arrows with ASCII arrows (`->`).
3. Replaced unicode ellipses with three ASCII dots (`...`).
4. Replaced unicode tree markers in documentation with ASCII pipe/hyphen characters.

## Changes Made
- AmbientContext.cs: replaced double-line header divider with ASCII equals signs.
- IJsonSerializer.cs: replaced box-drawing dividers across 12 section headers with standard ASCII hyphens.
- IBinarySerializer.cs, IStreamSerializer.cs, IStringSerializer.cs: replaced box-drawing dividers with ASCII hyphens.
- Emitron.cs and ScriptInternals.cs: normalized section dividers and arrows to ASCII.
- BlobManagerTests.cs: normalized 12 section divider comments and arrow to ASCII.
- MemoryCachingProxyTests.cs: normalized section divider comments to ASCII.
- CoreJsonSerializerTests.cs: normalized 15 section divider comments to ASCII.
- JsonNamingAttributeTests.cs: normalized section comments and translated Cyrillic word to English.
- ObjectJsonConverter.cs: converted arrows in comments to `->`.
- MathExtensions.cs and Image.cs: converted unicode ellipses to `...`.
- BufferGeometry.cs: converted section symbol to ASCII text.
- ActDim.Practix.Common/README.md, ActDim.Observability/README.md, and monorepo.md: normalized tree drawing characters to ASCII.

## Verification
- Solution-wide test run via `.along/scripts/test.py`: 12 test projects executed, 732 passed, 0 failures.
- Scanned repository to verify 0 box-drawing characters remain in active code and markdown files.
