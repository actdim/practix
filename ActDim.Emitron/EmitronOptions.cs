#nullable enable
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace ActDim.Emitron
{
    /// <summary>
    /// Configuration options for compiling and evaluating C# scripts with <see cref="Emitron"/>.
    /// </summary>
    public class EmitronOptions
    {
        /// <summary>
        /// Gets the collection of search directories used when resolving assembly references (<c>#r</c>)
        /// and script files (<c>#load</c>).
        /// </summary>
        public IList<string> SearchPaths { get; } = new List<string>();

        /// <summary>
        /// Gets the collection of <see cref="Assembly"/> instances referenced by default in compiled scripts.
        /// </summary>
        public IList<Assembly> Assemblies { get; } = new List<Assembly>();

        /// <summary>
        /// Gets the collection of assembly names or file paths referenced in compiled scripts.
        /// </summary>
        public IList<string> AssemblyReferences { get; } = new List<string>();

        /// <summary>
        /// Gets the collection of imported namespaces (<c>using Namespace;</c>) included by default in compiled scripts.
        /// </summary>
        public IList<string> Usings { get; } = new List<string>();

        /// <summary>
        /// Initializes a new instance of <see cref="EmitronOptions"/> with default search paths,
        /// assemblies, and usings.
        /// </summary>
        public EmitronOptions()
        {
            // Default search paths: AppContext base directory and runtime framework directory
            var baseDir = AppContext.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir))
            {
                SearchPaths.Add(baseDir);
            }

            var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
            if (!string.IsNullOrEmpty(runtimeDir) && !SearchPaths.Contains(runtimeDir, StringComparer.OrdinalIgnoreCase))
            {
                SearchPaths.Add(runtimeDir);
            }

            // Default referenced assemblies
            Assemblies.Add(typeof(object).Assembly);
            Assemblies.Add(typeof(Enumerable).Assembly);
            Assemblies.Add(typeof(ExpandoObject).Assembly);
            Assemblies.Add(typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly);

            // Default imported namespaces
            Usings.Add("System");
            Usings.Add("System.Linq");
            Usings.Add("System.Collections.Generic");
            Usings.Add("System.Dynamic");
        }

        /// <summary>
        /// Adds one or more directory paths to <see cref="SearchPaths"/>.
        /// </summary>
        /// <param name="paths">The directories to search for assemblies and scripts.</param>
        /// <returns>This <see cref="EmitronOptions"/> instance for method chaining.</returns>
        public EmitronOptions AddSearchPaths(params string[] paths)
        {
            if (paths != null)
            {
                foreach (var path in paths)
                {
                    if (!string.IsNullOrWhiteSpace(path) && !SearchPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
                    {
                        SearchPaths.Add(path);
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Adds one or more assemblies to <see cref="Assemblies"/>.
        /// </summary>
        /// <param name="assemblies">The assemblies to reference in scripts.</param>
        /// <returns>This <see cref="EmitronOptions"/> instance for method chaining.</returns>
        public EmitronOptions AddAssemblies(params Assembly[] assemblies)
        {
            if (assemblies != null)
            {
                foreach (var asm in assemblies)
                {
                    if (asm != null && !Assemblies.Contains(asm))
                    {
                        Assemblies.Add(asm);
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Adds the assemblies containing the specified <paramref name="types"/> to <see cref="Assemblies"/>.
        /// </summary>
        /// <param name="types">The types whose containing assemblies should be referenced.</param>
        /// <returns>This <see cref="EmitronOptions"/> instance for method chaining.</returns>
        public EmitronOptions AddAssemblies(params Type[] types)
        {
            if (types != null)
            {
                foreach (var type in types)
                {
                    if (type != null && !Assemblies.Contains(type.Assembly))
                    {
                        Assemblies.Add(type.Assembly);
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Adds one or more assembly names (e.g. <c>"System.Text.Json"</c>) or file paths to <see cref="AssemblyReferences"/>.
        /// </summary>
        /// <param name="assemblyNamesOrPaths">The assembly names or file paths to reference.</param>
        /// <returns>This <see cref="EmitronOptions"/> instance for method chaining.</returns>
        public EmitronOptions AddAssemblies(params string[] assemblyNamesOrPaths)
        {
            if (assemblyNamesOrPaths != null)
            {
                foreach (var name in assemblyNamesOrPaths)
                {
                    if (!string.IsNullOrWhiteSpace(name) && !AssemblyReferences.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        AssemblyReferences.Add(name);
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Adds one or more namespace names to <see cref="Usings"/>.
        /// </summary>
        /// <param name="namespaces">The namespaces to import in scripts.</param>
        /// <returns>This <see cref="EmitronOptions"/> instance for method chaining.</returns>
        public EmitronOptions AddUsings(params string[] namespaces)
        {
            if (namespaces != null)
            {
                foreach (var ns in namespaces)
                {
                    if (!string.IsNullOrWhiteSpace(ns) && !Usings.Contains(ns, StringComparer.Ordinal))
                    {
                        Usings.Add(ns);
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Creates a configured <see cref="ScriptOptions"/> instance based on this options object.
        /// </summary>
        /// <returns>A configured <see cref="ScriptOptions"/> instance.</returns>
        public ScriptOptions ToScriptOptions()
        {
            var baseDir = SearchPaths.FirstOrDefault() ?? AppContext.BaseDirectory;

            var options = ScriptOptions.Default
                .WithMetadataResolver(
                    ScriptMetadataResolver.Default
                        .WithBaseDirectory(baseDir)
                        .WithSearchPaths(SearchPaths))
                .WithSourceResolver(
                    ScriptSourceResolver.Default
                        .WithBaseDirectory(baseDir)
                        .WithSearchPaths(SearchPaths))
                .WithReferences(Assemblies)
                .WithImports(Usings)
                .WithOptimizationLevel(OptimizationLevel.Release);

            if (AssemblyReferences.Count > 0)
            {
                options = options.AddReferences(AssemblyReferences);
            }

            return options;
        }

        /// <summary>
        /// Creates a deep copy of this <see cref="EmitronOptions"/> instance.
        /// </summary>
        /// <returns>A cloned <see cref="EmitronOptions"/>.</returns>
        public EmitronOptions Clone()
        {
            var clone = new EmitronOptions();
            clone.SearchPaths.Clear();
            foreach (var p in SearchPaths)
            {
                clone.SearchPaths.Add(p);
            }

            clone.Assemblies.Clear();
            foreach (var r in Assemblies)
            {
                clone.Assemblies.Add(r);
            }

            clone.AssemblyReferences.Clear();
            foreach (var a in AssemblyReferences)
            {
                clone.AssemblyReferences.Add(a);
            }

            clone.Usings.Clear();
            foreach (var i in Usings)
            {
                clone.Usings.Add(i);
            }

            return clone;
        }
    }
}
