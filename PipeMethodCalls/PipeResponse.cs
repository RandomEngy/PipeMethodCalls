using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// A response sent over the pipe.
	/// </summary>
	internal class PipeResponse
	{
		/// <summary>
		/// The call ID.
		/// </summary>
		[JsonProperty]
		public long CallId { get; private set; }

		/// <summary>
		/// True if the call succeeded.
		/// </summary>
		[JsonProperty]
		public bool Succeeded { get; private set; }

		/// <summary>
		/// The response data. Valid if Succeeded is true.
		/// </summary>
		[JsonProperty]
		public object Data { get; private set; }

		/// <summary>
		/// The error details. Valid if Succeeded is false.
		/// </summary>
		[JsonProperty]
		public string Error { get; private set; }

		/// <summary>
		/// Creates a new success pipe response.
		/// </summary>
		/// <param name="callId">The ID of the call.</param>
		/// <param name="data">The returned data.</param>
		/// <returns></returns>
		public static PipeResponse Success(long callId, object data)
		{
			return new PipeResponse { Succeeded = true, CallId = callId, Data = data };
		}

		/// <summary>
		/// Creates a new failure pipe response.
		/// </summary>
		/// <param name="callId">The ID of the call.</param>
		/// <param name="message">The failure message.</param>
		/// <returns>The failure pipe response.</returns>
		public static PipeResponse Failure(long callId, string message)
		{
			return new PipeResponse { Succeeded = false, CallId = callId, Error = message };
		}
	}
}
