#region License

/*
 * Copyright � 2002-2011 Paul Borodaev.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

#region Imports

using System;
using System.Reflection;
using System.Reflection.Emit;
using ActDim.Practix.Collections.Concurrent;

#endregion

namespace ActDim.Practix.TypeAccess.Reflection
{
    using ModuleId = (string AssemblyName, string ModuleName);
    // public record ModuleId(string AssemblyName, string ModuleName);

    /// <summary>
    /// Use this class for obtaining <see cref="ModuleBuilder"/> instances for dynamic code generation.
    /// </summary>      
    /// <seealso cref="ActDim.Practix.TypeAccess.Reflection.DynamicReflectionManager"/>
    public sealed class DynamicCodeManager
    {
        private static readonly ConcurrentFactoryDictionary<string, AssemblyBuilder> AssemblyCache = new(CreateAssemblyBuilder);

        private static readonly ConcurrentFactoryDictionary<ModuleId, ModuleBuilder> ModuleCache = new(CreateModuleBuilder);

        private static AssemblyBuilder CreateAssemblyBuilder(string assemblyName)
        {
            var an = new AssemblyName
            {
                Name = assemblyName
            };

            var oAn = Assembly.GetExecutingAssembly().GetName();

            try
            {
                an.SetPublicKey(oAn.GetPublicKey());
            }
            catch
            {
            }

            try
            {
                an.SetPublicKeyToken(oAn.GetPublicKeyToken());
            }
            catch
            {
            }

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            // RunAndCollect?
            return assemblyBuilder;
        }

        private static ModuleBuilder CreateModuleBuilder(ModuleId id)
        {
            var assemblyBuilder = GetAssemblyBuilder(id.AssemblyName);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(id.ModuleName);
            return moduleBuilder;
        }

        /// <summary>
        /// prevent instantiation
        /// </summary>
        private DynamicCodeManager()
        {
            throw new InvalidOperationException();
        }

        public static string GetDynamicName(string tag)
        {
            var guid = Guid.NewGuid().ToString("N");
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"{tag}.{timestamp}_{guid}";
        }

        public static AssemblyBuilder GetAssemblyBuilder(string assemblyName)
        {
            return AssemblyCache.GetOrCreateValue(assemblyName);
        }

        /// <summary>
        /// Returns the <see cref="ModuleBuilder"/> for the dynamic moduleBuilder within the specified dynamic assembly.
        /// </summary>
        /// <remarks>
        /// If the assembly does not exist yet, it will be created.<br/>
        /// This factory caches any dynamic assembly it creates - calling GetModule() twice with 
        /// the same name will *not* create 2 distinct modules!
        /// </remarks>
        /// <param name="assemblyName">The assembly-name of the moduleBuilder to be returned</param>
        /// <returns>the <see cref="ModuleBuilder"/> that can be used to define new types within the specified assembly</returns>
        public static ModuleBuilder GetModuleBuilder(ModuleId moduleId)
        {
            return ModuleCache.GetOrCreateValue(moduleId);
        }

        /// <summary>
        /// Removes all registered <see cref="ModuleBuilder"/>s.
        /// </summary>
        public static void Clear()
        {
            ModuleCache.Clear();
        }
    }
}