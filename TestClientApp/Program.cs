using PipeMethodCalls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestAppCore;

namespace TestClientApp
{
	class Program
	{
		static void Main(string[] args)
		{
			RunClientAsync();
			Console.ReadKey();
		}

		private static async Task RunClientAsync()
		{
			var pipeClientWithCallback = new PipeClient<IAdder>("testpipe");
			pipeClientWithCallback.SetLogger(message => Console.WriteLine(message));

			await pipeClientWithCallback.ConnectAsync().ConfigureAwait(false);
			WrappedInt result = await pipeClientWithCallback.InvokeAsync(adder => adder.AddWrappedNumbers(new WrappedInt { Num = 1 }, new WrappedInt { Num = 3 })).ConfigureAwait(false);

			Console.WriteLine("Server add result: " + result.Num);

			await pipeClientWithCallback.WaitForRemotePipeCloseAsync();

			Console.WriteLine("Server closed pipe.");
		}
	}
}
