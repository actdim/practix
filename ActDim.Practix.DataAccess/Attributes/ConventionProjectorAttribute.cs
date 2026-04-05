using OrthoBits.Abstractions.DataAccess;
using System;
using System.Linq;

namespace OrthoBits.DataAccess.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ConventionProjectorAttribute : Attribute
    {
        public ConventionProjectorAttribute(Type conventionProjector)
        {
            if (conventionProjector.GetInterfaces().All(x => x != typeof(ISqlDialect)))
            {
                throw new Exception($"{nameof(conventionProjector)} must implement {nameof(ISqlDialect)}");
            }
            ConventionProjector = conventionProjector;
        }

        public Type ConventionProjector { get; }
    }
}
