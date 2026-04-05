namespace ActDim.Practix.Abstractions.Logging
{
    // WellKnownProperty/LogContextProperty
    public enum ContextProperty
    {
        Status,
        Progress,
        CallContext,
        Operation,
        Tag,
        EventCategory,
        EventSource, // SourceContext
        Application,
        CorrelationId,
        ParentCorrelationId
    }
}