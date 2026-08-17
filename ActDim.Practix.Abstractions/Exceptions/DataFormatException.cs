using System;

namespace ActDim.Practix.Abstractions.Exceptions
{
    /// <summary>
    /// Exception thrown when data in any form does not conform to the expected format, encoding, or structural specification.
    /// </summary>
    public class DataFormatException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataFormatException"/> class.
        /// </summary>
        public DataFormatException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataFormatException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message describing the error.</param>
        public DataFormatException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataFormatException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception cause.</param>
        public DataFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
