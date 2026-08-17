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
    /// <summary>
    /// Factory delegate for creating configured <see cref="JsonSerializerOptions"/> instances.
    /// </summary>
    /// <param name="forMerge">Indicates whether the options are intended for JSON object merging.</param>
    /// <returns>A new <see cref="JsonSerializerOptions"/> instance.</returns>
    public delegate JsonSerializerOptions JsonSerializerOptionsFactory(bool forMerge = false);

    /// <summary>
    /// Contract defining comprehensive JSON serialization, deserialization, object population, cloning, and document merging operations.
    /// </summary>
    public interface IJsonSerializer
    {
        /// <summary>
        /// Gets or sets the active default serializer options.
        /// </summary>
        JsonSerializerOptions Options { get; set; }

        /// <summary>
        /// Gets or sets the active JSON merge configuration options.
        /// </summary>
        JsonMergeOptions MergeOptions { get; set; }

        /// <summary>
        /// Creates an independent clone of this serializer instance.
        /// </summary>
        /// <returns>A new <see cref="IJsonSerializer"/> instance with copied configuration.</returns>
        IJsonSerializer Clone();

        /// <summary>
        /// Gets or sets the delegate factory used to generate default serializer options.
        /// </summary>
        JsonSerializerOptionsFactory DefaultOptionsFactory { get; set; }

        /// <summary>
        /// Creates a new instance of default <see cref="JsonSerializerOptions"/>.
        /// </summary>
        /// <returns>A configured <see cref="JsonSerializerOptions"/> instance.</returns>
        JsonSerializerOptions CreateDefaultOptions();

        /// <summary>
        /// Creates a new instance of default <see cref="JsonMergeOptions"/>.
        /// </summary>
        /// <returns>A configured <see cref="JsonMergeOptions"/> instance.</returns>
        JsonMergeOptions CreateDefaultMergeOptions();

        // ── Options helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Copies serializer options, converter registrations, and resolver chains from <paramref name="source"/> into <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The target options instance to update.</param>
        /// <param name="source">The source options instance. If <c>null</c>, uses <see cref="Options"/>.</param>
        void CopyOptions(JsonSerializerOptions target, JsonSerializerOptions source = null);

        // ── Serialize to string ──────────────────────────────────────────────

        /// <summary>
        /// Serializes the specified object value into a JSON string using default options.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <returns>The serialized JSON string.</returns>
        string Serialize(object value);

        /// <summary>
        /// Serializes the specified object value into a JSON string using custom options.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="options">The serializer options to apply. If <c>null</c>, uses default options.</param>
        /// <returns>The serialized JSON string.</returns>
        string Serialize(object value, JsonSerializerOptions options);

        // ── Merge & serialize ────────────────────────────────────────────────

        /// <summary>
        /// Merges multiple objects into a single JSON representation and serializes it using default options.
        /// </summary>
        /// <param name="values">The sequence of objects to merge sequentially.</param>
        /// <returns>The merged JSON string representation.</returns>
        string MergeAndSerialize(IList<object> values);

        /// <summary>
        /// Merges multiple objects into a single JSON representation and serializes it using custom serializer options.
        /// </summary>
        /// <param name="values">The sequence of objects to merge sequentially.</param>
        /// <param name="options">The serializer options to apply. If <c>null</c>, uses default options.</param>
        /// <returns>The merged JSON string representation.</returns>
        string MergeAndSerialize(IList<object> values, JsonSerializerOptions options);

        /// <summary>
        /// Merges multiple objects into a single JSON representation and serializes it using custom options and merge behavior.
        /// </summary>
        /// <param name="values">The sequence of objects to merge sequentially.</param>
        /// <param name="options">The serializer options to apply. If <c>null</c>, uses default options.</param>
        /// <param name="mergeOptions">The merge behavior options. If <c>null</c>, uses default merge options.</param>
        /// <returns>The merged JSON string representation.</returns>
        string MergeAndSerialize(IList<object> values, JsonSerializerOptions options, JsonMergeOptions mergeOptions);

        /// <summary>
        /// Merges multiple objects into a single JSON representation using custom merge behavior.
        /// </summary>
        /// <param name="values">The sequence of objects to merge sequentially.</param>
        /// <param name="mergeOptions">The merge behavior options. If <c>null</c>, uses default merge options.</param>
        /// <returns>The merged JSON string representation.</returns>
        string MergeAndSerialize(IList<object> values, JsonMergeOptions mergeOptions);

        // ── Serialize to stream (sync) ───────────────────────────────────────

        /// <summary>
        /// Synchronously serializes the specified object value to the provided stream using default options.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="stream">The target stream to write JSON into.</param>
        void Serialize(object value, Stream stream);

        /// <summary>
        /// Synchronously serializes the specified object value to the provided stream using custom options.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="stream">The target stream to write JSON into.</param>
        /// <param name="options">The serializer options to apply. If <c>null</c>, uses default options.</param>
        void Serialize(object value, Stream stream, JsonSerializerOptions options);

        // ── Serialize to stream (async) ──────────────────────────────────────

        /// <summary>
        /// Asynchronously serializes the specified object value to the provided stream using default options.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="stream">The target stream to write JSON into.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous serialization operation.</returns>
        Task SerializeAsync(object value, Stream stream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously serializes the specified object value to the provided stream using custom options.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="stream">The target stream to write JSON into.</param>
        /// <param name="options">The serializer options to apply. If <c>null</c>, uses default options.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous serialization operation.</returns>
        Task SerializeAsync(object value, Stream stream, JsonSerializerOptions options, CancellationToken cancellationToken = default);

        // ── Deserialize from string ──────────────────────────────────────────

        /// <summary>
        /// Deserializes the JSON string to an instance of the specified runtime type using default options.
        /// </summary>
        /// <param name="json">The JSON string to parse.</param>
        /// <param name="type">The expected destination runtime type.</param>
        /// <returns>The deserialized object instance.</returns>
        object Deserialize(string json, Type type);

        /// <summary>
        /// Deserializes the JSON string to an instance of the specified runtime type using custom options.
        /// </summary>
        /// <param name="json">The JSON string to parse.</param>
        /// <param name="type">The expected destination runtime type.</param>
        /// <param name="options">The serializer options to apply. If <c>null</c>, uses default options.</param>
        /// <returns>The deserialized object instance.</returns>
        object Deserialize(string json, Type type, JsonSerializerOptions options);

        /// <summary>
        /// Deserializes the JSON string to the specified type <typeparamref name="T"/> using default options.
        /// </summary>
        /// <typeparam name="T">The expected destination type.</typeparam>
        /// <param name="json">The JSON string to parse.</param>
        /// <returns>The deserialized instance of <typeparamref name="T"/>.</returns>
        T Deserialize<T>(string json);

        /// <summary>
        /// Deserializes the JSON string to the specified type <typeparamref name="T"/> using custom options.
        /// </summary>
        /// <typeparam name="T">The expected destination type.</typeparam>
        /// <param name="json">The JSON string to parse.</param>
        /// <param name="options">The serializer options to apply. If <c>null</c>, uses default options.</param>
        /// <returns>The deserialized instance of <typeparamref name="T"/>.</returns>
        T Deserialize<T>(string json, JsonSerializerOptions options);

        /// <summary>
        /// Deserializes the JSON string to the specified type <typeparamref name="T"/> applying custom converter instances.
        /// </summary>
        /// <typeparam name="T">The expected destination type.</typeparam>
        /// <param name="json">The JSON string to parse.</param>
        /// <param name="customConverters">Custom JSON converters to register for this deserialization call.</param>
        /// <returns>The deserialized instance of <typeparamref name="T"/>.</returns>
        T Deserialize<T>(string json, params JsonConverter[] customConverters);

        // ── Deserialize from stream (sync) ───────────────────────────────────

        /// <summary>
        /// Synchronously deserializes the stream content to an instance of the specified runtime type using default options.
        /// </summary>
        /// <param name="stream">The source stream containing JSON data.</param>
        /// <param name="type">The expected destination runtime type.</param>
        /// <returns>The deserialized object instance.</returns>
        object Deserialize(Stream stream, Type type);

        /// <summary>
        /// Synchronously deserializes the stream content to an instance of the specified runtime type using custom options.
        /// </summary>
        /// <param name="stream">The source stream containing JSON data.</param>
        /// <param name="type">The expected destination runtime type.</param>
        /// <param name="options">The serializer options to apply. If <c>null</c>, uses default options.</param>
        /// <returns>The deserialized object instance.</returns>
        object Deserialize(Stream stream, Type type, JsonSerializerOptions options);

        /// <summary>
        /// Synchronously deserializes the stream content to the specified type <typeparamref name="T"/> using default options.
        /// </summary>
        /// <typeparam name="T">The expected destination type.</typeparam>
        /// <param name="stream">The source stream containing JSON data.</param>
        /// <returns>The deserialized instance of <typeparamref name="T"/>.</returns>
        T Deserialize<T>(Stream stream);

        /// <summary>
        /// Synchronously deserializes the stream content to the specified type <typeparamref name="T"/> using custom options.
        /// </summary>
        /// <typeparam name="T">The expected destination type.</typeparam>
        /// <param name="stream">The source stream containing JSON data.</param>
        /// <param name="options">The serializer options to apply. If <c>null</c>, uses default options.</param>
        /// <returns>The deserialized instance of <typeparamref name="T"/>.</returns>
        T Deserialize<T>(Stream stream, JsonSerializerOptions options);

        // ── Deserialize from stream (async) ──────────────────────────────────

        /// <summary>
        /// Asynchronously deserializes the stream content to an instance of the specified runtime type using default options.
        /// </summary>
        /// <param name="stream">The source stream containing JSON data.</param>
        /// <param name="type">The expected destination runtime type.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A value task representing the asynchronous operation with the deserialized object.</returns>
        ValueTask<object> DeserializeAsync(Stream stream, Type type, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deserializes the stream content to an instance of the specified runtime type using custom options.
        /// </summary>
        /// <param name="stream">The source stream containing JSON data.</param>
        /// <param name="type">The expected destination runtime type.</param>
        /// <param name="options">The serializer options to apply. If <c>null</c>, uses default options.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A value task representing the asynchronous operation with the deserialized object.</returns>
        ValueTask<object> DeserializeAsync(Stream stream, Type type, JsonSerializerOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deserializes the stream content to the specified type <typeparamref name="T"/> using default options.
        /// </summary>
        /// <typeparam name="T">The expected destination type.</typeparam>
        /// <param name="stream">The source stream containing JSON data.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A value task representing the asynchronous operation with the deserialized instance of <typeparamref name="T"/>.</returns>
        ValueTask<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deserializes the stream content to the specified type <typeparamref name="T"/> using custom options.
        /// </summary>
        /// <typeparam name="T">The expected destination type.</typeparam>
        /// <param name="stream">The source stream containing JSON data.</param>
        /// <param name="options">The serializer options to apply. If <c>null</c>, uses default options.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A value task representing the asynchronous operation with the deserialized instance of <typeparamref name="T"/>.</returns>
        ValueTask<T> DeserializeAsync<T>(Stream stream, JsonSerializerOptions options, CancellationToken cancellationToken = default);

        // ── Populate ─────────────────────────────────────────────────────────

        /// <summary>
        /// Populates writable properties and fields of the existing <paramref name="target"/> object instance from JSON data.
        /// </summary>
        /// <typeparam name="T">The type of the target object.</typeparam>
        /// <param name="json">The JSON data providing values.</param>
        /// <param name="target">The target instance to populate.</param>
        void Populate<T>(string json, T target);

        /// <summary>
        /// Populates writable properties and fields of the existing <paramref name="target"/> object instance from JSON data using custom options.
        /// </summary>
        /// <typeparam name="T">The type of the target object.</typeparam>
        /// <param name="json">The JSON data providing values.</param>
        /// <param name="target">The target instance to populate.</param>
        /// <param name="options">The serializer options to apply. If <c>null</c>, uses default options.</param>
        void Populate<T>(string json, T target, JsonSerializerOptions options);

        // ── Naming helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Formats a full path property name according to the active property naming policy.
        /// </summary>
        /// <param name="fullPathPropertyName">The full path property name (e.g., "Parent.ChildProperty").</param>
        /// <returns>The policy-transformed property name.</returns>
        string FormatPropertyName(string fullPathPropertyName);

        /// <summary>
        /// Formats a full path property name according to the naming policy specified in <paramref name="options"/>.
        /// </summary>
        /// <param name="fullPathPropertyName">The full path property name (e.g., "Parent.ChildProperty").</param>
        /// <param name="options">The serializer options providing the naming policy.</param>
        /// <returns>The policy-transformed property name.</returns>
        string FormatPropertyName(string fullPathPropertyName, JsonSerializerOptions options);

        // ── Object utilities ─────────────────────────────────────────────────

        /// <summary>
        /// Creates a deep clone of the specified source object graph via JSON round-trip serialization.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="source">The source object to clone.</param>
        /// <returns>A new deep copy of the source object graph.</returns>
        T Clone<T>(T source);

        /// <summary>
        /// Creates a deep clone of the specified source object graph via JSON round-trip serialization using custom options.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="source">The source object to clone.</param>
        /// <param name="options">The serializer options to apply.</param>
        /// <returns>A new deep copy of the source object graph.</returns>
        T Clone<T>(T source, JsonSerializerOptions options);

        /// <summary>
        /// Deeply copies values from <paramref name="from"/> into the existing object instance <paramref name="to"/>.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="to">The target object receiving values.</param>
        /// <param name="from">The source object providing values.</param>
        /// <returns>The modified <paramref name="to"/> instance.</returns>
        T Copy<T>(T to, T from);

        /// <summary>
        /// Applies a partial JSON patch document onto the specified target object instance.
        /// </summary>
        /// <typeparam name="T">The target object type.</typeparam>
        /// <param name="obj">The target object instance to patch.</param>
        /// <param name="patch">The partial JSON string providing patch values.</param>
        /// <returns>The modified object instance with patch applied.</returns>
        T Patch<T>(T obj, string patch);

        // ── Serialize to bytes ─────────────────────────────────────────────────

        /// <summary>
        /// Serializes the specified object value directly to a UTF-8 byte array using default options.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <returns>A byte array containing the UTF-8 encoded JSON representation.</returns>
        byte[] SerializeToBytes(object value);

        /// <summary>
        /// Serializes the specified object value directly to a UTF-8 byte array using custom options.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="options">The serializer options to apply.</param>
        /// <returns>A byte array containing the UTF-8 encoded JSON representation.</returns>
        byte[] SerializeToBytes(object value, JsonSerializerOptions options);

        /// <summary>
        /// Serializes the specified object value to a byte array using custom options and text encoding.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="options">The serializer options to apply.</param>
        /// <param name="encoding">The text encoding to use. If <c>null</c>, defaults to <see cref="Encoding.UTF8"/>.</param>
        /// <returns>A byte array containing the encoded JSON representation.</returns>
        byte[] SerializeToBytes(object value, JsonSerializerOptions options, Encoding encoding = default);
    }
}
