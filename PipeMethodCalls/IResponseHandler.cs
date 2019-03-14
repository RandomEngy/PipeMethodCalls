using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// Handles a response message received from a remote endpoint.
	/// </summary>
	internal interface IResponseHandler
	{
		void HandleResponse(PipeResponse response);
	}
}
