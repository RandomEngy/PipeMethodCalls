using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	internal class PipeRequest
	{
		[JsonProperty]
		public long CallId { get; set; }

		[JsonProperty]
		public string MethodName { get; set; }

		[JsonProperty]
		public object[] Parameters { get; set; }

		[JsonProperty]
		public Type[] GenericArguments { get; set; }
	}
}
