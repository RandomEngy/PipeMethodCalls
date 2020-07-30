using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// A request sent over the pipe.
	/// </summary>
	internal class PipeRequest
	{
		/// <summary>
		/// The call ID.
		/// </summary>
		[JsonProperty]
		public long CallId { get; set; }

		/// <summary>
		/// The name of the method to invoke.
		/// </summary>
		[JsonProperty]
		public string MethodName { get; set; }

		/// <summary>
		/// The list of parameters to pass to the method.
		/// </summary>
		[JsonProperty]
		public object[] Parameters { get; set; }

		/// <summary>
		/// The types for the generic arguments.
		/// </summary>
		[JsonProperty]
		public Type[] GenericArguments { get; set; }
	}
}
