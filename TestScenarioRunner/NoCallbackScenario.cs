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
    public static class NoCallbackScenario
    {
		public static async Task RunClientAsync(PipeClient<IAdder> pipeClient)
		{
			await pipeClient.ConnectAsync().ConfigureAwait(false);
			WrappedInt result = await pipeClient.InvokeAsync(adder => adder.AddWrappedNumbers(new WrappedInt { Num = 1 }, new WrappedInt { Num = 3 })).ConfigureAwait(false);
			result.Num.ShouldBe(4);

			pipeClient.Dispose();
		}

		public static async Task RunServerAsync(PipeServer<IAdder> pipeServer)
		{
			await pipeServer.WaitForConnectionAsync();
			await pipeServer.WaitForRemotePipeCloseAsync();
		}
	}
}
