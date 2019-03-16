using System;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	public interface IPipeServer
	{
		void SetLogger(Action<string> logger);
		Task WaitForConnectionAsync(CancellationToken cancellationToken = default);
		Task WaitForRemotePipeCloseAsync(CancellationToken cancellationToken = default);
	}
}