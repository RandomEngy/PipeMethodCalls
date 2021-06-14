using PipeMethodCalls;
using PipeMethodCalls.NetJson;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestAppCore;

namespace TestCallbackClientApp
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
			var rawStream = new NamedPipeClientStream(".", "testpipe", PipeDirection.InOut, PipeOptions.Asynchronous);
			var pipeClientWithCallback = new PipeClientWithCallback<IAdder, IConcatenator>(new NetJsonPipeSerializer(), rawStream, () => new Concatenator());

			//var pipeClientWithCallback = new PipeClientWithCallback<IAdder, IConcatenator>(new NetJsonPipeSerializer(), "testpipe", () => new Concatenator());
			pipeClientWithCallback.SetLogger(message => Console.WriteLine(message));

			try
			{
				await pipeClientWithCallback.ConnectAsync().ConfigureAwait(false);
				WrappedInt result = await pipeClientWithCallback.InvokeAsync(adder => adder.AddWrappedNumbers(new WrappedInt { Num = 1 }, new WrappedInt { Num = 3 })).ConfigureAwait(false);
				Console.WriteLine("Server wrapped add result: " + result.Num);

				int asyncResult = await pipeClientWithCallback.InvokeAsync(adder => adder.AddAsync(4, 7)).ConfigureAwait(false);
				Console.WriteLine("Server async add result: " + asyncResult);

				IList<string> listifyResult = await pipeClientWithCallback.InvokeAsync(adder => adder.Listify("item")).ConfigureAwait(false);
				Console.WriteLine("Server listify result: " + listifyResult[0]);

				try
				{
					await pipeClientWithCallback.InvokeAsync(adder => adder.AlwaysFails()).ConfigureAwait(false);
				}
				catch (PipeInvokeFailedException exception)
				{
					Console.WriteLine("Handled invoke exception:" + Environment.NewLine + exception);
				}

				try
				{
					int refValue = 4;
					await pipeClientWithCallback.InvokeAsync(adder => adder.HasRefParam(ref refValue)).ConfigureAwait(false);
				}
				catch (PipeInvokeFailedException exception)
				{
					Console.WriteLine("Handled invoke exception:" + Environment.NewLine + exception);
				}

				await pipeClientWithCallback.WaitForRemotePipeCloseAsync();

				Console.WriteLine("Server closed pipe.");
			}
			catch (Exception exception)
			{
				Console.WriteLine("Exception in pipe processing: " + exception);
			}
		}
	}
}
