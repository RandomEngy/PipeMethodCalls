using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	internal class PipeResponse
	{
		[JsonProperty]
		public long CallId { get; private set; }

		[JsonProperty]
		public bool Succeeded { get; private set; }

		[JsonProperty]
		public object Data { get; private set; }

		[JsonProperty]
		public string Error { get; private set; }

		public static PipeResponse Failure(long callId, string message)
		{
			return new PipeResponse { Succeeded = false, CallId = callId, Error = message };
		}

		public static PipeResponse Success(long callId, object data)
		{
			return new PipeResponse { Succeeded = true, CallId = callId, Data = data };
		}
	}
}
