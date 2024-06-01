using PipeMethodCalls;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore;

namespace TestScenarioRunner
{
	public static class ServerCrashScenario
	{
		public static async Task RunClientAsync(PipeClient<ICrasher> pipeClient)
		{
			await pipeClient.ConnectAsync().ConfigureAwait(false);
			await Should.ThrowAsync<IOException>(async () =>
			{
				await pipeClient.InvokeAsync(crasher => crasher.Crash()).ConfigureAwait(false);
			});

			pipeClient.Dispose();
		}

		public static async Task RunServerAsync(PipeServer<ICrasher> pipeServer)
		{
			await pipeServer.WaitForConnectionAsync();
			await pipeServer.WaitForRemotePipeCloseAsync();
		}
	}
}
