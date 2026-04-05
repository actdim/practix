namespace ActDim.Practix.Abstractions.Patterns // ActDim.Practix.Abstractions.DesignPatterns or ActDim.Practix.Abstractions.Core
{
    // Originating design pattern

    public interface IProvider<out TResult>
    {
        TResult Get();
    }

    public interface IProvider<out TResult, in TArg>
    {
        TResult Get(TArg arg);
    }

    public interface IProvider<out TResult, in TArg1, in TArg2>
    {
        TResult Get(TArg1 arg1, TArg2 arg2);
    }

    public interface IProvider<out TResult, in TArg1, in TArg2, in TArg3>
    {
        TResult Get(TArg1 arg1, TArg2 arg2, TArg3 arg3);
    }

    public interface IProvider<out TResult, in TArg1, in TArg2, in TArg3, in TArg4>
    {
        TResult Get(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4);
    }

    public interface IProvider<out TResult, in TArg1, in TArg2, in TArg3, in TArg4, in TArg5>
    {
        TResult Get(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5);
    }
}