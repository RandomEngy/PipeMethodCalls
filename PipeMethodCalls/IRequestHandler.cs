using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// Handles a request message received from a remote endpoint.
	/// </summary>
	internal interface IRequestHandler
	{
		void HandleRequest(PipeRequest request);
	}
}
