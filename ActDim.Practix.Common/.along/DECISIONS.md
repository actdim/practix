# Decisions (ADR: append-only)

_One dated entry per architectural decision. Never edit past entries; mark a replaced one "Superseded by #N"._

<!-- Template:
## #001: <title>
- Date: YYYY-MM-DD
- Status: accepted            (or: superseded by #NNN)
- Context: <why this came up>
- Decision: <what was decided>
- Consequences: <trade-offs / follow-ups>
-->

## #001: Compression coverage is BCL-only; declared-but-unsupported formats throw
- Date: 2026-08-04
- Status: accepted
- Context: `CompressionFormat` declares GZip, Brotli, Deflate, BZip2, LZMA, LZMA2, PPMd and `ArchiveFormat`
  declares Zip, SevenZip, Rar, Tar. The .NET 10 base class library ships codecs for only GZip / Deflate /
  Brotli / ZIP / TAR. Covering the rest means taking a third-party dependency (SharpCompress or similar).
- Decision: implement `CompressionManager` against the BCL only. Signature *detection* covers every format we
  can recognize (BZip2, LZMA, 7z, RAR included), but any attempt to actually compress/decompress an
  unsupported format throws `NotSupportedException` naming the supported set. No third-party codec is added to
  `ActDim.Practix.Common`.
- Consequences: `ActDim.Practix.Common` keeps its current dependency set. Detection can legitimately return a
  format that the same object cannot process: deliberate: "what is this?" and "can I read it?" are different
  questions. Adding a codec later means a subclass or a sibling implementation, not a rewrite: the
  encoder/decoder factories (`CreateCompressionStream` / `CreateDecompressionStream`) are the single
  extension point.

## #002: Stream ownership and rewind contract
- Date: 2026-08-04
- Status: accepted
- Context: the legacy (commented-out) code rewound `outputStream` to 0 even when the caller had supplied it,
  which silently breaks append/compose scenarios, and it was unclear who disposes what.
- Decision: (a) a stream `CompressionManager` CREATES is returned rewound to 0 and is owned by the caller
  (dispose returns the pooled blocks); (b) a destination the CALLER supplies is written from its current
  position and is never rewound and never closed: except in `CompressToArchiveAsync(Stream outputStream, …)`,
  whose declared contract is to hand that stream back, so it is rewound; (c) an input stream is always
  consumed as a whole (rewound first when seekable) and left open; (d) entry streams handed to a
  reader/writer callback are owned by the manager and disposed as soon as the callback returns, and may be
  opened at most once per entry (`InvalidOperationException` otherwise): mandatory for ZIP, where an entry is
  only finalized when its stream closes.
- Consequences: callbacks must consume/write entry data before returning (documented on the members);
  composing several writes into one caller stream works; on failure the manager disposes any stream the caller
  will never receive, so pooled blocks are not leaked.

## #003: `using` declarations, not `using` statements, for resource scoping
- Date: 2026-08-04
- Status: accepted
- Context: `CompressionManager` had up to four nested `using (…) { }` blocks (archive → entry stream → source
  stream), pushing real logic 5-6 levels deep for no benefit. The root `AGENTS.md` rule "always brace
  `if`/`else`/`for`/`foreach`/`while`/`do`/`using`: never single-line or same-line bodies" is about statement
  BODIES; a `using` *declaration* (`using var x = …;`) has no body at all, so it is not what that rule targets.
  `Extensions/StreamExtensions.cs` already used the declaration form.
- Decision: prefer the declaration form `using var x = …;` / `await using var x = …;` for resource scoping.
  Keep the block form only where the resource must be released before code that follows it in the same scope
  (i.e. where disposal order genuinely differs from "end of enclosing block").
- Consequences: the whole of `CompressionManager` is now at most two levels deep. Two things to keep in mind:
  (a) inside a loop body a `using` declaration disposes at the END OF EACH ITERATION, which is what the ZIP
  writer relies on (an entry is only finalized when its stream closes, before the next `CreateEntry`);
  (b) multiple declarations in one scope dispose in reverse declaration order: same as nesting.
  Do not "restore" the braces citing the general bracing rule; that rule is about bodies.
