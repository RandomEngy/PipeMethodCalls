using System;

namespace PipeMethodCalls
{
    internal static class ExceptionBuilder
    {
        /// <summary>
        /// Creates an <see cref="ArgumentException"/> with informative error message when pipe cannot be wrapped with method call capability.
        /// </summary>
        public static ArgumentException InvalidRawPipeException()
        {
            return new ArgumentException("Provided pipe cannot be wrapped.  Pipe needs to be setup with the following: PipeDirection.InOut, PipeOptions.Asynchronous, and PipeTransmissionMode.Byte");
        }
    }
}