using Autofac;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ActDim.Practix.Autofac
{
    /// <summary>
    /// Base Autofac module that tracks registered module types on <see cref="ContainerBuilder.Properties"/> to guarantee idempotent single-execution.
    /// </summary>
    public abstract class TrackableModule : Module
    {
        private const string LoadedModulesKey = "__MODULES__";

        /// <summary>
        /// Derived classes override this method to perform Autofac registrations exactly once.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        protected abstract void LoadOnce(ContainerBuilder builder);

        /// <inheritdoc />
        protected override void Load(ContainerBuilder builder)
        {
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
            var modules = ImmutableHashSet.Create(moduleSet.ToArray());
            builder.Properties[LoadedModulesKey] = modules;
        }
    }
}
