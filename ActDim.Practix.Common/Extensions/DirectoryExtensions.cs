
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
    public static class DirectoryExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="errHandler">onError</param>
        /// <returns></returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public static IEnumerable<Assembly> LoadAssemblies(string path, Func<Exception, bool> errHandler = default)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"The directory '{path}' does not exist.");
            }

            var dllFiles = Directory.GetFiles(path, "*.dll");

            foreach (var dllFile in dllFiles)
            {
                Assembly assembly = default;
                try
                {
                    assembly = Assembly.LoadFrom(dllFile);
                }
                catch (Exception ex)
                {
                    if (errHandler != default)
                    {
                        // $"Failed to load assembly from '{dllFile}': {ex.Message}"
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