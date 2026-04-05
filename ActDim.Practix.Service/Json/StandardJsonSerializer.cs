using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Extensions;
using ActDim.Practix.Memory;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace ActDim.Practix.Service.Json
{
    // TODO: check [EnumeratorCancellation], JsonExtensionData, JsonSerializerContext
    internal class StandardJsonSerializer : IJsonSerializer
    {
        private JsonSerializerOptions _options;
        private JsonMergeOptions _mergeOptions;

        public JsonSerializerOptions Options
        {
            get
            {
                return _options;
            }
            set
            {
                _options = value;
            }
        }

        public JsonMergeOptions MergeOptions
        {
            get
            {
                return _mergeOptions;
            }
            set
            {
                _mergeOptions = value;
            }
        }

        private JsonNodeOptions _nodeOptions;
        private JsonDocumentOptions _docOptions;

        public JsonSerializerOptionsFactory DefaultOptionsFactory { get; set; }

        public JsonSerializerOptions CreateDefaultOptions()
        {
            return DefaultOptionsFactory != null ? DefaultOptionsFactory(false) : CreateDefaultOptions(false);
        }

        public JsonMergeOptions CreateDefaultMergeOptions()
        {
            return new JsonMergeOptions
            {
                BaseOptions = DefaultOptionsFactory != null ? DefaultOptionsFactory(true) : CreateDefaultOptions(true),
                MergeArrayHandling = JsonMergeArrayHandling.Replace,
                MergeNullValueHandling = JsonMergeNullValueHandling.Merge
            };
        }

        public StandardJsonSerializer() : this(default, default)
        {
        }

        public StandardJsonSerializer(JsonSerializerOptions options = default) : this(options, default)
        {
        }

        public StandardJsonSerializer(JsonMergeOptions mergeOptions = default) : this(default, mergeOptions)
        {
        }

        public StandardJsonSerializer(JsonSerializerOptions options = default, JsonMergeOptions mergeOptions = default)
        {
            _options = options ?? CreateDefaultOptions();
            _mergeOptions = mergeOptions ?? CreateDefaultMergeOptions();
            _nodeOptions = new JsonNodeOptions
            {
                PropertyNameCaseInsensitive = _options.PropertyNameCaseInsensitive
            };
            _docOptions = new JsonDocumentOptions
            {
                AllowTrailingCommas = _options.AllowTrailingCommas,
                CommentHandling = _options.ReadCommentHandling,
                MaxDepth = _options.MaxDepth
            };
        }

        public IJsonSerializer Clone()
        {
            var options = CreateDefaultOptions();
            var mergeOptions = CreateDefaultMergeOptions();
            return new StandardJsonSerializer(options, mergeOptions);
        }

        private JsonSerializerOptions CreateDefaultOptions(bool forMerge = false)
        {
            var options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                PropertyNamingPolicy = null,
                DictionaryKeyPolicy = null,
                PropertyNameCaseInsensitive = true,
                // DefaultIgnoreCondition = forMerge ? JsonIgnoreCondition.Never : JsonIgnoreCondition.WhenWritingNull,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                WriteIndented = false,
                IncludeFields = true,
                // STJ's JavaScriptEncoder.Default - aggressive escaping (like StringEscapeHandling = StringEscapeHandling.EscapeNonAscii in Newtonsoft JSON)
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // similar to Newtonsoft JSON defaults
                UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
                // ReferenceHandler = ReferenceHandler.Preserve, // strict
                ReferenceHandler = ReferenceHandler.IgnoreCycles, // safe                
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow // [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
            };

            var resolver = new DefaultJsonTypeInfoResolver();

            // we can't use JsonTypeInfoResolver.Combine because it uses first non-null result (resolver)
            resolver.Modifiers.Add(NamingPolicyResolver.Apply);
            resolver.Modifiers.Add(DefaultValueAwareResolver.Apply);
            resolver.Modifiers.Add(EmptyCollectionIgnoreResolver.Apply);
            options.TypeInfoResolver = resolver;

            // options.Converters.Add(new JsonStringEnumConverter(null));            
            // options.Converters.Add(new CustomDateTimeConverter());

            options.Converters.Add(new NewtonsoftCompatibleStringConverter());
            options.Converters.Add(new NumberEnumConverterFactory());
            options.Converters.Add(new FloatingPointConverterFactory());
            // handles Exception types (must be before ObjectJsonConverter)
            options.Converters.Add(new ExceptionJsonConverter());
            // handles concrete types with implicit ops
            options.Converters.Add(new ImplicitOperatorConverterFactory());
            // handles object properties
            options.Converters.Add(new ObjectJsonConverter());

            return options;
        }

        public void CopyOptions(
            JsonSerializerOptions target,
            JsonSerializerOptions source = default)
        {
            source ??= _options;
            target.AllowTrailingCommas = source.AllowTrailingCommas;
            target.DefaultBufferSize = source.DefaultBufferSize;
            target.DefaultIgnoreCondition = source.DefaultIgnoreCondition;
            target.DictionaryKeyPolicy = source.DictionaryKeyPolicy;
            target.Encoder = source.Encoder;
            target.IgnoreReadOnlyFields = source.IgnoreReadOnlyFields;
            target.IgnoreReadOnlyProperties = source.IgnoreReadOnlyProperties;
            target.IncludeFields = source.IncludeFields;
            target.IndentCharacter = source.IndentCharacter;
            target.IndentSize = source.IndentSize;
            target.MaxDepth = source.MaxDepth;
            target.NewLine = source.NewLine;
            target.NumberHandling = source.NumberHandling;
            target.PreferredObjectCreationHandling = source.PreferredObjectCreationHandling;
            target.PropertyNameCaseInsensitive = source.PropertyNameCaseInsensitive;
            target.PropertyNamingPolicy = source.PropertyNamingPolicy;
            target.ReadCommentHandling = source.ReadCommentHandling;
            target.ReferenceHandler = source.ReferenceHandler;
            target.RespectNullableAnnotations = source.RespectNullableAnnotations;
            target.RespectRequiredConstructorParameters = source.RespectRequiredConstructorParameters;
            target.TypeInfoResolver = source.TypeInfoResolver;
            target.UnknownTypeHandling = source.UnknownTypeHandling;
            target.UnmappedMemberHandling = source.UnmappedMemberHandling;
            target.WriteIndented = source.WriteIndented;

            foreach (var resolver in source.TypeInfoResolverChain)
            {
                if (!target.TypeInfoResolverChain.Contains(resolver))
                    target.TypeInfoResolverChain.Add(resolver);
            }

            foreach (var converter in source.Converters)
            {
                if (!target.Converters.Contains(converter))
                {
                    target.Converters.Add(converter);
                }
            }
        }

        public string Serialize(object value)
        {
            return JsonSerializer.Serialize(value, _options);
        }

        public string Serialize(object value, JsonSerializerOptions options)
        {
            options ??= _options;
            return JsonSerializer.Serialize(value, options);
        }

        public string MergeAndSerialize(IList<object> values)
        {
            return MergeAndSerialize(values, _options, _mergeOptions);
        }

        public string MergeAndSerialize(IList<object> values, JsonSerializerOptions options)
        {
            return MergeAndSerialize(values, options, _mergeOptions);
        }

        public string MergeAndSerialize(
            IList<object> values,
            JsonSerializerOptions options,
            JsonMergeOptions mergeOptions)
        {
            options ??= _options;
            mergeOptions ??= _mergeOptions;

            if (values == null || values.Count == 0)
            {
                return "{}";
            }

            if (values.Count == 1 && values[0] != null)
            {
                return Serialize(values[0], options);
            }

            JsonNode container = null;

            foreach (var value in values)
            {
                if (value == null) continue;

                var jsonString = JsonSerializer.Serialize(value, _mergeOptions.BaseOptions);
                var jsonNode = JsonNode.Parse(jsonString);

                if (container == null)
                {
                    container = jsonNode;
                }
                else
                {
                    MergeJsonNodes(container, jsonNode, mergeOptions);
                }
            }

            if (container == null)
            {
                return "{}";
            }

            return container.ToJsonString(options);
        }

        public string MergeAndSerialize(IList<object> values, JsonMergeOptions mergeOptions)
        {
            return MergeAndSerialize(values, _options, mergeOptions);
        }

        public void Serialize(object value, Stream stream)
        {
            Serialize(value, stream, _options);
        }

        public void Serialize(object value, Stream stream, JsonSerializerOptions options)
        {
            options ??= _options;
            JsonSerializer.Serialize(stream, value, options);
        }

        public async Task SerializeAsync(
            object value,
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            await SerializeAsync(value, stream, _options, cancellationToken);
        }

        public Task SerializeAsync(
            object value,
            Stream stream,
            JsonSerializerOptions options,
            CancellationToken cancellationToken = default)
        {
            options ??= _options;
            return JsonSerializer.SerializeAsync(stream, value, options, cancellationToken);
        }

        public object Deserialize(string json, Type type)
        {
            return Deserialize(json, type, _options);
        }

        public object Deserialize(string json, Type type, JsonSerializerOptions options)
        {
            options ??= _options;
            return JsonSerializer.Deserialize(json, type, options);
        }

        public T Deserialize<T>(string json)
        {
            return Deserialize<T>(json, _options);
        }

        public T Deserialize<T>(string json, JsonSerializerOptions options)
        {
            options ??= _options;
            return JsonSerializer.Deserialize<T>(json, options);
        }

        public T Deserialize<T>(string json, params JsonConverter[] customConverters)
        {
            return Deserialize<T>(json, _options);
        }

        public object Deserialize(Stream stream, Type type)
        {
            return Deserialize(stream, type, _options);
        }

        public object Deserialize(Stream stream, Type type, JsonSerializerOptions options)
        {
            options ??= _options;
            return JsonSerializer.Deserialize(stream, type, options);
        }

        public T Deserialize<T>(Stream stream)
        {
            return Deserialize<T>(stream, _options);
        }

        public T Deserialize<T>(Stream stream, JsonSerializerOptions options)
        {
            options ??= _options;
            return JsonSerializer.Deserialize<T>(stream, options);
        }

        public ValueTask<object> DeserializeAsync(
            Stream stream,
            Type type,
            CancellationToken cancellationToken = default)
        {
            return DeserializeAsync(stream, type, _options, cancellationToken);
        }

        public ValueTask<object> DeserializeAsync(
            Stream stream,
            Type type,
            JsonSerializerOptions options,
            CancellationToken cancellationToken = default)
        {
            options ??= _options;
            return JsonSerializer.DeserializeAsync(stream, type, options, cancellationToken);
        }

        public ValueTask<T> DeserializeAsync<T>(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            return DeserializeAsync<T>(stream, _options, cancellationToken);
        }

        public ValueTask<T> DeserializeAsync<T>(
            Stream stream,
            JsonSerializerOptions options,
            CancellationToken cancellationToken = default)
        {
            options ??= _options;
            return JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken);
        }

        public void Populate<T>(string json, T target)
        {
            Populate(json, target, _options);
        }

        public void Populate<T>(string json, T target, JsonSerializerOptions options)
        {
            if (target == null)
            {
                return;
            }

            options ??= _options;
            var jsonNode = JsonNode.Parse(json);

            if (jsonNode is JsonObject jsonObj)
            {
                MergeJsonObjectIntoObject(jsonObj, target, options);
            }
        }

        private void MergeJsonObjectIntoObject<T>(
            JsonObject sourceJson,
            T targetObj,
            JsonSerializerOptions options)
        {
            options ??= _options;
            var targetType = typeof(T);
            var properties = targetType.GetProperties();

            foreach (var prop in properties)
            {
                if (!prop.CanWrite) continue;

                var jsonPropertyName = options.PropertyNamingPolicy?.ConvertName(prop.Name) ?? prop.Name;

                if (sourceJson.ContainsKey(jsonPropertyName))
                {
                    var jsonValue = sourceJson[jsonPropertyName];
                    if (jsonValue is null)
                    {
                        prop.SetValue(targetObj, null);
                    }
                    else
                    {
                        var value = JsonSerializer.Deserialize(
                            jsonValue.ToJsonString(),
                            prop.PropertyType,
                            options);
                        prop.SetValue(targetObj, value);
                    }
                }
            }
        }

        public string FormatPropertyName(string fullPathPropertyName)
        {
            return FormatPropertyName(fullPathPropertyName, _options);
        }

        public string FormatPropertyName(string fullPathPropertyName, JsonSerializerOptions options)
        {
            if (string.IsNullOrEmpty(fullPathPropertyName))
            {
                return fullPathPropertyName;
            }

            options ??= _options;

            var parts = fullPathPropertyName.Split('.');
            var builder = new StringBuilder();

            for (int i = 0; i < parts.Length; i++)
            {
                var converted = options.PropertyNamingPolicy?.ConvertName(parts[i]) ?? parts[i];
                builder.Append(converted);

                if (i != parts.Length - 1)
                    builder.Append('.');
            }

            return builder.ToString();
        }

        public T Clone<T>(T source)
        {
            return Clone<T>(source, _options);
        }

        public T Clone<T>(T source, JsonSerializerOptions options)
        {
            if (source is null)
            {
                return default;
            }

            var json = Serialize(source, options);
            return Deserialize<T>(json, options);
        }

        public T Copy<T>(T to, T from)
        {
            if (from is null)
            {
                return to;
            }

            var json = Serialize(from);
            Populate(json, to);
            return to;
        }

        public T Patch<T>(T obj, string patch)
        {
            if (string.IsNullOrEmpty(patch))
            {
                return obj;
            }

            Populate(patch, obj);
            return obj;
        }

        private void MergeJsonNodes(
            JsonNode target,
            JsonNode source,
            JsonMergeOptions mergeOptions)
        {
            if (target is JsonObject targetObj && source is JsonObject sourceObj)
            {
                foreach (var prop in sourceObj)
                {
                    if (targetObj.ContainsKey(prop.Key))
                    {
                        var targetValue = targetObj[prop.Key];
                        var sourceValue = prop.Value;

                        if (targetValue is JsonObject targetSubObj && sourceValue is JsonObject sourceSubObj)
                        {
                            MergeJsonNodes(targetSubObj, sourceSubObj, mergeOptions);
                        }
                        else if (targetValue is JsonArray targetArray && sourceValue is JsonArray sourceArray)
                        {
                            MergeJsonArrays(targetArray, sourceArray, mergeOptions);
                        }
                        else if (sourceValue == null)
                        {
                            if (mergeOptions.MergeNullValueHandling ==
                                JsonMergeNullValueHandling.Merge)
                            {
                                targetObj[prop.Key] = null;
                            }
                        }
                        else
                        {
                            targetObj[prop.Key] = sourceValue?.DeepClone();
                        }
                    }
                    else
                    {
                        targetObj[prop.Key] = source[prop.Key]?.DeepClone();
                    }
                }
            }
        }

        private void MergeJsonArrays(
            JsonArray target,
            JsonArray source,
            JsonMergeOptions mergeOptions)
        {
            switch (mergeOptions.MergeArrayHandling)
            {
                case JsonMergeArrayHandling.Replace:
                    target.Clear();
                    foreach (var item in source)
                    {
                        target.Add(item?.DeepClone());
                    }
                    break;

                case JsonMergeArrayHandling.Concat:
                    foreach (var item in source)
                    {
                        target.Add(item?.DeepClone());
                    }
                    break;
                case JsonMergeArrayHandling.Merge:
                    for (int i = 0; i < source.Count; i++)
                    {
                        if (i < target.Count)
                        {
                            if (target[i] is JsonObject targetObj && source[i] is JsonObject sourceObj)
                            {
                                MergeJsonNodes(targetObj, sourceObj, mergeOptions);
                            }
                            else
                            {
                                target[i] = source[i]?.DeepClone();
                            }
                        }
                        else
                        {
                            target.Add(source[i]?.DeepClone());
                        }
                    }
                    break;

                case JsonMergeArrayHandling.Union:
                    var existingValues = new HashSet<string>();
                    foreach (var item in target)
                    {
                        existingValues.Add(item.ToJsonString());
                    }

                    foreach (var item in source)
                    {
                        var itemJson = item.ToJsonString();
                        if (!existingValues.Contains(itemJson))
                        {
                            target.Add(item?.DeepClone());
                        }
                    }
                    break;
            }
        }

        public Task<T> CloneAsync<T>(T source)
        {
            return CloneAsync(source, _options);
        }

        public async Task<T> CloneAsync<T>(T source, JsonSerializerOptions options, CancellationToken cancellationToken = default)
        {
            if (source is null)
            {
                return default;
            }

            using (var ms = MemoryManager.Default.GetContextStream())
            {
                await SerializeAsync(source, ms, options, cancellationToken);
                ms.Position = 0L;
                return await DeserializeAsync<T>(ms, options, cancellationToken);
            }
        }

        public byte[] SerializeToBytes(object value)
        {
            return SerializeToBytes(value, default, default);
        }

        public byte[] SerializeToBytes(object value, JsonSerializerOptions options)
        {
            return SerializeToBytes(value, options, default);
        }

        public byte[] SerializeToBytes(object value, JsonSerializerOptions options, Encoding encoding = default)
        {
            if (encoding == null || encoding == Encoding.UTF8)
            {
                return JsonSerializer.SerializeToUtf8Bytes(value, options);
            }
            else
            {
                var str = Serialize(value, options);
                return encoding.GetBytes(str);
            }
        }
    }
}
