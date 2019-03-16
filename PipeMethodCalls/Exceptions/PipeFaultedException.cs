using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// Represents an error that caused the pipe to become faulted.
	/// </summary>
	public class PipeFaultedException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PipeFaultedException"/> class.
		/// </summary>
		/// <param name="message">The exception message.</param>
		public PipeFaultedException(string message) 
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeFaultedException"/> class.
		/// </summary>
		/// <param name="message">The exception message.</param>
		/// <param name="innerException">The inner exception.</param>
		public PipeFaultedException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
