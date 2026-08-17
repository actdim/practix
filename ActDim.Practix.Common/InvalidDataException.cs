using System;

namespace ActDim.Practix.Common
{
    /// <summary>
    /// Exception thrown when data fails domain validation or structural checks.
    /// </summary>
    public class InvalidDataException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDataException"/> class.
        /// </summary>
        public InvalidDataException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDataException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message describing the error.</param>
        public InvalidDataException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDataException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception cause.</param>
        public InvalidDataException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
