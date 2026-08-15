using ActDim.Practix.Collections.Concurrent;
using Ardalis.GuardClauses;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace ActDim.Practix.TypeAccess.Reflection
{
    using ModuleId = (string AssemblyName, string ModuleName);

    /// <summary>
    /// Thread-safe manager for creating, obtaining, and caching dynamic assemblies (<see cref="AssemblyBuilder"/>)
    /// and dynamic modules (<see cref="ModuleBuilder"/>) for runtime code generation.
    /// </summary>
    public sealed class DynamicCodeManager
    {
        private static readonly ConcurrentFactoryDictionary<string, AssemblyBuilder> AssemblyCache = new(CreateAssemblyBuilder);
        private static readonly ConcurrentFactoryDictionary<ModuleId, ModuleBuilder> ModuleCache = new(CreateModuleBuilder);

        /// <summary>
        /// Prevents direct instantiation.
        /// </summary>
        private DynamicCodeManager()
        {
            throw new InvalidOperationException("DynamicCodeManager is a static manager class.");
        }

        /// <summary>
        /// Generates a unique name for a dynamic assembly or module combining a prefix tag, timestamp, and GUID.
        /// </summary>
        /// <param name="tag">A descriptive prefix tag (e.g. namespace or component name).</param>
        /// <returns>A unique formatted dynamic name string.</returns>
        public static string GetDynamicName(string tag)
        {
            Guard.Against.NullOrEmpty(tag, nameof(tag));
            var guid = Guid.NewGuid().ToString("N");
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"{tag}.{timestamp}_{guid}";
        }

        /// <summary>
        /// Gets or creates a cached <see cref="AssemblyBuilder"/> for the specified assembly name.
        /// </summary>
        /// <param name="assemblyName">The name of the dynamic assembly.</param>
        /// <returns>The cached or created <see cref="AssemblyBuilder"/>.</returns>
        public static AssemblyBuilder GetAssemblyBuilder(string assemblyName)
        {
            Guard.Against.NullOrEmpty(assemblyName, nameof(assemblyName));
            return AssemblyCache.GetOrCreateValue(assemblyName);
        }

        /// <summary>
        /// Gets or creates a cached <see cref="ModuleBuilder"/> for the specified module identifier.
        /// </summary>
        /// <param name="moduleId">A tuple containing the assembly name and module name.</param>
        /// <returns>The cached or created <see cref="ModuleBuilder"/>.</returns>
        public static ModuleBuilder GetModuleBuilder(ModuleId moduleId)
        {
            Guard.Against.NullOrEmpty(moduleId.AssemblyName, nameof(moduleId.AssemblyName));
            Guard.Against.NullOrEmpty(moduleId.ModuleName, nameof(moduleId.ModuleName));
            return ModuleCache.GetOrCreateValue(moduleId);
        }

        /// <summary>
        /// Gets or creates a cached <see cref="ModuleBuilder"/> for the specified assembly and module names.
        /// </summary>
        /// <param name="assemblyName">The name of the dynamic assembly.</param>
        /// <param name="moduleName">The name of the dynamic module.</param>
        /// <returns>The cached or created <see cref="ModuleBuilder"/>.</returns>
        public static ModuleBuilder GetModuleBuilder(string assemblyName, string moduleName)
        {
            return GetModuleBuilder((assemblyName, moduleName));
        }

        /// <summary>
        /// Clears all cached dynamic modules and assemblies.
        /// </summary>
        public static void Clear()
        {
            ModuleCache.Clear();
            AssemblyCache.Clear();
        }

        private static AssemblyBuilder CreateAssemblyBuilder(string assemblyName)
        {
            var an = new AssemblyName
            {
                Name = assemblyName
            };

            var executingAssembly = Assembly.GetExecutingAssembly().GetName();

            try
            {
                var publicKey = executingAssembly.GetPublicKey();
                if (publicKey != null && publicKey.Length > 0)
                {
                    an.SetPublicKey(publicKey);
                }
            }
            catch
            {
            }

            try
            {
                var token = executingAssembly.GetPublicKeyToken();
                if (token != null && token.Length > 0)
                {
                    an.SetPublicKeyToken(token);
                }
            }
            catch
            {
            }

            return AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
        }

        private static ModuleBuilder CreateModuleBuilder(ModuleId id)
        {
            var assemblyBuilder = GetAssemblyBuilder(id.AssemblyName);
            return assemblyBuilder.DefineDynamicModule(id.ModuleName);
        }
    }
}
