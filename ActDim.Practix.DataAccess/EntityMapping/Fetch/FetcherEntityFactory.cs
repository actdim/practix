using Autofac;
using FastMember;
using OrthoBits.Abstractions.DataAccess.Generic;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace OrthoBits.DataAccess.EntityMapping.Fetch
{
    public class FetcherEntityFactory
    {
        private static readonly Dictionary<Type, Func<object>> _defaultFactoryCache;

        static FetcherEntityFactory()
        {
            _defaultFactoryCache = new Dictionary<Type, Func<object>>();
        }

        private readonly Dictionary<Type, Func<object>> _customFactoryCache;
        private readonly ILifetimeScope _scope;

        public FetcherEntityFactory(ILifetimeScope scope)
        {
            _scope = scope;
            _customFactoryCache = new Dictionary<Type, Func<object>>();
        }

        public object CreateEntity(Type type)
        {
            if (_customFactoryCache.TryGetValue(type, out var cachedFactory))
            {
                return cachedFactory();
            }
            var factoryType = typeof(IDbEntityFactory<>).MakeGenericType(type);
            if (_scope.IsRegistered(factoryType))
            {
                var customFactoryInstance = _scope.Resolve(factoryType);
                var method = factoryType.GetMethod(nameof(IDbEntityFactory<object>.CreateInstance));
                var caller = Expression.Convert(
                    Expression.Call(Expression.Constant(customFactoryInstance), method),
                    typeof(object));
                var customFactoryMethod = Expression
                    .Lambda<Func<object>>(caller).Compile();
                _customFactoryCache[type] = customFactoryMethod;
                return customFactoryMethod();
            }
            var defaultFactory = new Func<object>(() =>
            {
                var accessor = TypeAccessor.Create(type, true);
                return accessor.CreateNew();
            });
            _customFactoryCache[type] = defaultFactory;
            return defaultFactory();
        }
    }
}