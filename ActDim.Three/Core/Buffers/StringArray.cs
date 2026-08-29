namespace ActDim.Three.Core.Buffers
{
    /// <summary>
    /// Custom (non-three.js) typed buffer backed by a <c>string[]</c>. three.js has no such TypedArray and
    /// its loader will not build a <c>*BufferAttribute</c> from it - this exists to carry string payloads
    /// through the C# model and JSON round-trip for our own consumers, not for the standard renderer.
    /// </summary>
    public sealed class StringArray : TypedArray<string>
    {
        public override string Type => TypedArrays.StringArray;
    }
}
