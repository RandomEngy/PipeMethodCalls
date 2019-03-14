using PipeMethodCalls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestAppCore;

namespace TestNetClientApp
{
	class Program
	{
		private static PipeClientWithCallback<IAdder, IConcatenator> pipeClientWithCallback;
		private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

		static void Main(string[] args)
		{
			pipeClientWithCallback = new PipeClientWithCallback<IAdder, IConcatenator>("testpipe");

			RunClientAsync(pipeClientWithCallback);

			Console.ReadKey();
			cancellationTokenSource.Cancel();
		}

		private static async Task RunClientAsync(PipeClientWithCallback<IAdder, IConcatenator> client)
		{
			await pipeClientWithCallback.ConnectAsync(() => new Concatenator(), cancellationTokenSource.Token).ConfigureAwait(false);
			WrappedInt result = await pipeClientWithCallback.InvokeAsync(adder => adder.AddWrappedNumbers(new WrappedInt { Num = 1 }, new WrappedInt { Num = 3 })).ConfigureAwait(false);

			Console.WriteLine("Server add result: " + result.Num);
		}
	}
}
