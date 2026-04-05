using Autofac;
using System.Collections.Immutable;

namespace ActDim.Practix.Autofac
{
    public abstract class TrackableModule : Module
    {
        const string LoadedModulesKey = "__MODULES__";
        protected abstract void LoadOnce(ContainerBuilder builder);

        protected override void Load(ContainerBuilder builder)
        {
            ImmutableHashSet<Type> modules;
            HashSet<Type> moduleSet;
            if (builder.Properties.TryGetValue(LoadedModulesKey, out object obj))
            {
                moduleSet = new HashSet<Type>((ImmutableHashSet<Type>)obj);
            }
            else
            {
                moduleSet = new HashSet<Type>();
            }
            if (moduleSet.Contains(GetType()))
            {
                return;
            }
            LoadOnce(builder);
            moduleSet.Add(GetType());
            modules = ImmutableHashSet.Create(moduleSet.ToArray());
            builder.Properties[LoadedModulesKey] = modules;
        }
    }
}
