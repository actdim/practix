using System;

namespace ActDim.Practix.Common
{
    /// <summary>
    /// Exception thrown when payload data is incomplete or truncated.
    /// </summary>
    public class IncompleteDataException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IncompleteDataException"/> class.
        /// </summary>
        public IncompleteDataException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IncompleteDataException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public IncompleteDataException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IncompleteDataException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception cause.</param>
        public IncompleteDataException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}