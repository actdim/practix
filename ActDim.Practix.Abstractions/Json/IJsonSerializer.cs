using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Abstractions.Json
{
    public delegate JsonSerializerOptions JsonSerializerOptionsFactory(bool forMerge = false);

    public interface IJsonSerializer
    {
        JsonSerializerOptions Options { get; set; }

        JsonMergeOptions MergeOptions { get; set; }

        IJsonSerializer Clone();

        JsonSerializerOptionsFactory DefaultOptionsFactory { get; set; }

        JsonSerializerOptions CreateDefaultOptions();

        JsonMergeOptions CreateDefaultMergeOptions();

        // ── Options helpers ──────────────────────────────────────────────────

        void CopyOptions(JsonSerializerOptions target, JsonSerializerOptions source = null);

        // ── Serialize to string ──────────────────────────────────────────────

        string Serialize(object value);

        string Serialize(object value, JsonSerializerOptions options);

        // ── Merge & serialize ────────────────────────────────────────────────

        string MergeAndSerialize(IList<object> values);

        string MergeAndSerialize(IList<object> values, JsonSerializerOptions options);

        string MergeAndSerialize(IList<object> values, JsonSerializerOptions options, JsonMergeOptions mergeOptions);

        string MergeAndSerialize(IList<object> values, JsonMergeOptions mergeOptions);

        // ── Serialize to stream (sync) ───────────────────────────────────────

        void Serialize(object value, Stream stream);

        void Serialize(object value, Stream stream, JsonSerializerOptions options);

        // ── Serialize to stream (async) ──────────────────────────────────────

        Task SerializeAsync(object value, Stream stream, CancellationToken cancellationToken = default);

        Task SerializeAsync(object value, Stream stream, JsonSerializerOptions options, CancellationToken cancellationToken = default);

        // ── Deserialize from string ──────────────────────────────────────────

        object Deserialize(string json, Type type);

        object Deserialize(string json, Type type, JsonSerializerOptions options);

        T Deserialize<T>(string json);

        T Deserialize<T>(string json, JsonSerializerOptions options);

        T Deserialize<T>(string json, params JsonConverter[] customConverters);

        // ── Deserialize from stream (sync) ───────────────────────────────────

        object Deserialize(Stream stream, Type type);

        object Deserialize(Stream stream, Type type, JsonSerializerOptions options);

        T Deserialize<T>(Stream stream);

        T Deserialize<T>(Stream stream, JsonSerializerOptions options);

        // ── Deserialize from stream (async) ──────────────────────────────────

        ValueTask<object> DeserializeAsync(Stream stream, Type type, CancellationToken cancellationToken = default);

        ValueTask<object> DeserializeAsync(Stream stream, Type type, JsonSerializerOptions options, CancellationToken cancellationToken = default);

        ValueTask<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default);

        ValueTask<T> DeserializeAsync<T>(Stream stream, JsonSerializerOptions options, CancellationToken cancellationToken = default);

        // ── Populate ─────────────────────────────────────────────────────────

        void Populate<T>(string json, T target);

        void Populate<T>(string json, T target, JsonSerializerOptions options);

        // ── Naming helpers ───────────────────────────────────────────────────

        string FormatPropertyName(string fullPathPropertyName);

        string FormatPropertyName(string fullPathPropertyName, JsonSerializerOptions options);

        // ── Object utilities ─────────────────────────────────────────────────

        T Clone<T>(T source);

        T Clone<T>(T source, JsonSerializerOptions options);

        T Copy<T>(T to, T from);

        T Patch<T>(T obj, string patch);

        Task<T> CloneAsync<T>(T source);

        Task<T> CloneAsync<T>(T source, JsonSerializerOptions options, CancellationToken cancellationToken = default);

        // ── Serialize to bytes ─────────────────────────────────────────────────

        byte[] SerializeToBytes(object value);

        byte[] SerializeToBytes(object value, JsonSerializerOptions options);

        byte[] SerializeToBytes(object value, JsonSerializerOptions options, Encoding encoding = default);
    }
}
