namespace ActDim.Practix.Abstractions.Mapping
{
    public interface IMapper
    {
        // void Map<T>(T src, T dst);

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src">from</param>
        /// <param name="dst">to</param>
        void Copy<T>(T src, T dst);

        // T Map<T>(T src) where T: new();

        T Clone<T>(T src) where T : new();
    }
}
