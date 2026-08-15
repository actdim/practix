using System;
using System.Collections.Generic;

namespace ActDim.Practix.Abstractions.Context
{
    /// <summary>
    /// Represents an ambient execution context property bag backed by copy-on-write immutable dictionary storage.
    /// Values set here are scoped to the current asynchronous execution flow.
    /// </summary>
    public interface IAmbientContext
    {
        /// <summary>
        /// Gets the current ambient context properties of the asynchronous flow.
        /// </summary>
        IReadOnlyDictionary<string, object> Properties { get; }

        /// <summary>
        /// Pushes <paramref name="value"/> under <paramref name="name"/> into the ambient execution context.
        /// If <paramref name="name"/> already exists, its value is overridden for the duration of the returned handle.
        /// Disposing the returned handle restores the previous value (or removes the key if it was absent).
        /// </summary>
        IDisposable PushProperty(string name, object value);
    }
}
