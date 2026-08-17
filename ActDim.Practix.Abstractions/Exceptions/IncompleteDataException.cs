using System;

namespace ActDim.Practix.Abstractions.Exceptions
{
    /// <summary>
    /// Exception thrown when data in any form is incomplete, truncated, or missing required elements or segments.
    /// </summary>
    public class IncompleteDataException : DataFormatException
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
        /// <param name="message">The message describing the error.</param>
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
