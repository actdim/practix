namespace ActDim.Practix.Abstractions.Logging
{
    public interface ILoggerProvider
    {
        ILogger Get(string categoryName);
        IScopedLogger GetScoped(string categoryName);
    }
}