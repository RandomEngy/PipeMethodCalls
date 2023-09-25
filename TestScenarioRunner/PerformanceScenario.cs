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
    public static class PerformanceScenario
    {
		public static async Task RunClientAsync(PipeClient<IAdder> pipeClient)
		{
			await pipeClient.ConnectAsync().ConfigureAwait(false);

            double[] numbers = new[] {
                1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0,
                1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0,
                1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0,
                1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0 };

            for (int i = 0; i < 100_000; i++)
            {
                var result = pipeClient.Invoker.InvokeAsync(i => i.Sum(numbers));
            }

			pipeClient.Dispose();
		}

		public static async Task RunServerAsync(PipeServer<IAdder> pipeServer)
        {
            await pipeServer.WaitForConnectionAsync();
    
			await pipeServer.WaitForRemotePipeCloseAsync();
		}
	}
}
