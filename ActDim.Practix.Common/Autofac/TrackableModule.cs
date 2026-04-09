using Autofac;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
                moduleSet = [.. (ImmutableHashSet<Type>)obj];
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
