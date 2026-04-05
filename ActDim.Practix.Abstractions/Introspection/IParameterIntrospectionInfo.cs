namespace ActDim.Practix.Abstractions.Introspection
{
    public interface IParameterIntrospectionInfo : IParameterBaseIntrospectionInfo
    {
        /// <summary>
        /// Owner
        /// </summary>
        IBaseIntrospectionInfo Member { get; }        
    }
}