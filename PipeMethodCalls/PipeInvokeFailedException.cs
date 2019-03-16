using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// Represents a failed invoke on the remote endpoint.
	/// </summary>
	public class PipeInvokeFailedException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PipeInvokeFailedException"/> class.
		/// </summary>
		/// <param name="message">The exception message.</param>
		public PipeInvokeFailedException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeInvokeFailedException"/> class.
		/// </summary>
		/// <param name="message">The exception message.</param>
		/// <param name="innerException">The inner exception.</param>
		public PipeInvokeFailedException(string message, Exception innerException) : base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeInvokeFailedException"/> class.
		/// </summary>
		/// <param name="info">Serialization info.</param>
		/// <param name="context">Streaming context.</param>
		protected PipeInvokeFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
