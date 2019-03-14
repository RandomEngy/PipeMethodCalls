using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	internal class PendingCall
	{
		public TaskCompletionSource<PipeResponse> TaskCompletionSource { get; } = new TaskCompletionSource<PipeResponse>();
	}
}
