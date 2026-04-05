using SalientBits;
using System.Linq.Expressions;

namespace CanarySystems.Caching
{
    public interface IFuncCachingProxyFactory<TDelegate> : IFactory<TDelegate, Expression<TDelegate>>
    {
        // TDelegate Create(Expression<TDelegate> expression);
    }
}