using System.Threading.Tasks;

namespace ActDim.Practix.Abstractions.Patterns // ActDim.Practix.Abstractions.DesignPatterns or ActDim.Practix.Abstractions.Core
{
    // Originating design pattern

    public interface IAsyncProvider<TResult>
    {
        Task<TResult> GetAsync();
    }

    public interface IAsyncProvider<TResult, in TArg>
    {
        Task<TResult> GetAsync(TArg arg);
    }

    public interface IAsyncProvider<TResult, in TArg1, in TArg2>
    {
        Task<TResult> GetAsync(TArg1 arg1, TArg2 arg2);
    }

    public interface IAsyncProvider<TResult, in TArg1, in TArg2, in TArg3>
    {
        Task<TResult> GetAsync(TArg1 arg1, TArg2 arg2, TArg3 arg3);
    }

    public interface IAsyncProvider<TResult, in TArg1, in TArg2, in TArg3, in TArg4>
    {
        Task<TResult> GetAsync(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4);
    }

    public interface IAsyncProvider<TResult, in TArg1, in TArg2, in TArg3, in TArg4, in TArg5>
    {
        Task<TResult> GetAsync(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5);
    }
}
