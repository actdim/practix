using ActDim.Practix.Abstractions.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ActDim.Practix.Config
{
    /// <inheritdoc />
    public class JsonConfigurationManager : IJsonConfigurationManager
    {
        /// <summary>
        /// Default JSON Schema reference URI.
        /// </summary>
        public const string DefaultSchema = "http://json-schema.org/latest/json-schema-core.html#rfc.section.9.1";

        private const int BufferSize = 4 * 1024;
        private const FileOptions InputFileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
        private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(1);
        private const int AttemptDelay = 100;

        private readonly IJsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonConfigurationManager"/> class with the specified serializer.
        /// </summary>
        /// <param name="serializer">The JSON serializer implementation.</param>
        public JsonConfigurationManager(IJsonSerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc />
        public async Task SaveAsync<T>(T options, string path) where T : class
        {
            var json = _serializer.Serialize(options);

            Exception error = null;
            var timeout = LockTimeout.TotalMilliseconds;
            while (timeout > 0)
            {
                try
                {
                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true))
                    {
                        using (var sw = new StreamWriter(fs))
                        {
                            await sw.WriteAsync(json);
                            return;
                        }
                    }
                }
                catch (IOException ex)
                {
                    error = ex;
                }

                await Task.Delay(AttemptDelay);
                timeout -= AttemptDelay;
            }

            if (error != null)
            {
                throw error;
            }
        }

        /// <inheritdoc />
        public async Task<T> LoadAsync<T>(string path) where T : class, new()
        {
            return await LoadInternalAsync<T>(path);
        }

        private async Task<T> LoadInternalAsync<T>(string path, Action<string> validator = null) where T : class, new()
        {
            if (!File.Exists(path))
            {
                var obj = new T();
                await SaveAsync(obj, path);
                return obj;
            }

            Exception error = null;
            var timeout = LockTimeout.TotalMilliseconds;
            while (timeout > 0)
            {
                try
                {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, InputFileOptions))
                    {
                        using (var sr = new StreamReader(fs))
                        {
                            var json = await sr.ReadToEndAsync();
                            if (validator != null)
                            {
                                validator(json);
                            }
                            return _serializer.Deserialize<T>(json);
                        }
                    }
                }
                catch (IOException ex)
                {
                    error = ex;
                }

                await Task.Delay(AttemptDelay);
                timeout -= AttemptDelay;
            }

            if (error != null)
            {
                throw error;
            }

            return null;
        }
    }
}
