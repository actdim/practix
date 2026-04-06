using ActDim.Practix.Abstractions.Messaging;
using ActDim.Practix.Disposal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace ActDim.Practix.Messaging // ActDim.Practix.CallContext
{
    internal class CallContext : MarshalByRefObject, ICallContext
    {
        private ImmutableDictionary<string, object> _data;

        public CallContext()
        {
            _data = ImmutableDictionary.Create<string, object>(); // StringComparer.OrdinalIgnoreCase
        }

        /// <summary>
        /// Disposal will unset (remove) the value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IDisposable Set([NotNull] string name, object value) // Push
        {
            object oldValue = default;

            var overwrite = _data.TryGetValue(name, out oldValue);

            if (overwrite) // existed/existing
            {                
                oldValue = value;
            }

            _data = _data.SetItem(name, value);

            return new DisposableAction(() =>
            {
                if (overwrite)
                {
                    _data = _data.SetItem(name, oldValue);
                }
                else
                {
                    _data = _data.Remove(name);
                }
            });
        }

        public IReadOnlyDictionary<string, object> Data => _data.AsReadOnly();
    }
}