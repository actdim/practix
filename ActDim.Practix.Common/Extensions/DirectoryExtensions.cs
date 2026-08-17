using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ActDim.Practix.Extensions
{
    /// <summary>
    /// Extension methods for file system directory operations.
    /// </summary>
    public static class DirectoryExtensions
    {
        /// <summary>
        /// Loads all assemblies (`*.dll`) found in the specified directory path.
        /// </summary>
        /// <param name="path">The directory path containing assembly DLL files.</param>
        /// <param name="errHandler">An optional error handler callback. Returns true to swallow and continue loading; false to break iteration.</param>
        /// <returns>An enumerable sequence of successfully loaded assemblies.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if <paramref name="path"/> does not exist.</exception>
        public static IEnumerable<Assembly> LoadAssemblies(string path, Func<Exception, bool> errHandler = default)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"The directory '{path}' does not exist.");
            }

            var dllFiles = Directory.GetFiles(path, "*.dll");

            foreach (var dllFile in dllFiles)
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(dllFile);
                }
                catch (Exception ex)
                {
                    if (errHandler != default)
                    {
                        if (errHandler(ex))
                        {
                            continue;
                        }

                        break;
                    }

                    throw;
                }

                yield return assembly;
            }
        }
    }
}
