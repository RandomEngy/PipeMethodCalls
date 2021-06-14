using System;

namespace PipeMethodCalls.Models
{
    internal class PipeException
    {
        /// <summary>
        /// The exception. Valid if Succeeded is false.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// The exception type. Valid if Succeeded is false.
        /// </summary>
        public string ExceptionType { get; private set; }

        /// <summary>
        /// The exception message. Valid if Succeeded is false.
        /// </summary>
        public string ExceptionMessage { get; private set; }

        /// <summary>
        /// The exception stack. Valid if Succeeded is false.
        /// </summary>
        public string ExceptionStack { get; private set; }

        /// <summary>
        /// The inner exception. Valid if Succeeded is false.
        /// </summary>
        public PipeException InnerException { get; private set; }

        /// <summary>
        /// Creates a new failure PipeException.
        /// </summary>
        /// <param name="ex">The ID of the call.</param>
        /// <returns>The failure pipe response.</returns>
        public static PipeException Failure(Exception ex)
        {
            return new PipeException
            {
                Exception = ex,
                ExceptionType = ex.GetType().FullName,
                ExceptionMessage = ex.Message,
                ExceptionStack = ex.StackTrace,
                InnerException = ex.InnerException != null ? Failure(ex.InnerException) : null
            };
        }
    }
}
