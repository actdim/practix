using System.Linq.Expressions;
using ActDim.Practix.Abstractions.Patterns;

namespace ActDim.Practix.Abstractions.Caching
{
    public interface IFuncCachingProxyFactory<TDelegate>: IProvider<TDelegate, Expression<TDelegate>>
	{
		// TDelegate Create(Expression<TDelegate> expression);
	}
}