
using ActDim.Practix.Abstractions.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ActDim.Practix.Config
{
    public class JsonConfigurationManager : IJsonConfigurationManager
    {
        public const string DefaultSchema = "http://json-schema.org/latest/json-schema-core.html#rfc.section.9.1";

        private const int BufferSize = 4 * 1024;

        private const FileOptions InputFileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;

        private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(1);

        private const int AttemptDelay = 100;

        private readonly IJsonSerializer _serializer;

        public JsonConfigurationManager(IJsonSerializer serializer)
        {
            _serializer = serializer;
        }

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
                        // Encoding.UTF8
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
                        using (var sr = new StreamReader(fs)) // Encoding.UTF8
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

        public async Task<T> LoadAsync<T>(string path) where T : class, new()
        {
            return await LoadInternalAsync<T>(path);
        }

        /*
        public async Task<T> LoadAsync<T>(string path) where T : class, new()
        {
            var schema = LoadSchema<T>();
            return await LoadInternalAsync<T>(path, json => ValidateSchema(json, schema));
        }

        public async Task<T> LoadAsync<T>(string path, string schemaPath) where T : class, new()
        {
            var schema = await LoadSchemaAsync(schemaPath);
            return await LoadInternalAsync<T>(path, json => ValidateSchema(json, schema));
        }

        static async Task<JsonSchema> LoadSchemaAsync(string schemaPath)
        {
            var schema = await JsonSchema.FromFileAsync(schemaPath);
            return schema;
        }

        private JsonSchema LoadSchema<T>()
        {
            var schema = JsonSchema.FromType<T>(_serializer.SchemaGeneratorSettings);

            return ConfigureSchema<T>(schema);
        }

        private static JsonSchema ConfigureSchema<T>(JsonSchema schema)
        {
            schema.SchemaVersion = DefaultSchema;
            schema.AllowAdditionalProperties = true;
            foreach (var schema4 in schema.Definitions)
            {
                schema4.Value.AllowAdditionalProperties = true;
            }

            return schema;
        }

        private void ValidateSchema(string json, JsonSchema schema)
        {
            var errors = schema.Validate(json);
            if (errors != null && errors.Count > 0)
            {
                var errorInfo = string.Join("\n",
                errors.Select(x => $"Path: {x.Path}, Property: {x.Property}, Kind: {x.Kind}"));
                throw new FormatException($"Invalid configuration format:\n{errorInfo}");
            }

        */
    }
}
