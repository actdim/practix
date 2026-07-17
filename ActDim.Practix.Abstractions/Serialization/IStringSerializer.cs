using System;

namespace ActDim.Practix.Abstractions.Serialization
{
    /// <summary>Serializes objects to and from their textual (string) representation.</summary>
    public interface IStringSerializer
    {
        // ── Serialize to string ──────────────────────────────────────────────

        string Serialize(object value);

        string Serialize(object value, Type type);

        string Serialize<T>(T value);

        // ── Deserialize from string ──────────────────────────────────────────

        object Deserialize(string data, Type type);

        T Deserialize<T>(string data);
    }
}
