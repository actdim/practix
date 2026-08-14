using System;
using System.Collections.Generic;

namespace ActDim.Practix.Abstractions.Context
{
    /// <summary>
    /// Represents an ambient execution context property bag backed by copy-on-write immutable dictionary storage.
    /// <para>
    /// <b>Key Architectural Difference vs <c>IExternalScopeProvider</c>:</b><br/>
    /// <see cref="ICallContext"/> operates on a <i>Key-Value Dictionary Model</i>. Pushing an existing property name
    /// immediately updates/overrides the specific value in place for the current async flow, and disposing the handle
    /// restores the exact previous value. Unlike <c>IExternalScopeProvider</c> (which appends raw scope objects onto a stack),
    /// <see cref="ICallContext"/> maintains a single, active, updated dictionary state at all times.
    /// </para>
    /// </summary>
    public interface ICallContext
    {
        /// <summary>
        /// Pushes <paramref name="value"/> under <paramref name="name"/> into the ambient execution context.
        /// If <paramref name="name"/> already exists, its value is overridden for the duration of the returned handle.
        /// Disposing the returned handle restores the previous value (or removes the key if it was absent).
        /// </summary>
        IDisposable Push(string name, object value);

        /// <summary>
        /// Gets the current ambient context properties as a flat dictionary.
        /// </summary>
        IReadOnlyDictionary<string, object> Data { get; }
    }
}
