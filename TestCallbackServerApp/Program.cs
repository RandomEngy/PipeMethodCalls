using PipeMethodCalls;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestAppCore;

namespace TestCallbackServerApp
{
	class Program
	{
		static void Main(string[] args)
		{
			RunServerAsync();

			Console.ReadKey();
		}

		private static async Task RunServerAsync()
		{
			var rawPipeStream = new NamedPipeServerStream("testpipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
			var pipeServerWithCallback = new PipeServerWithCallback<IConcatenator, IAdder>(rawPipeStream, () => new Adder());

			//var pipeServerWithCallback = new PipeServerWithCallback<IConcatenator, IAdder>("testpipe", () => new Adder());
			pipeServerWithCallback.SetLogger(message => Console.WriteLine(message));

			try
			{
				await pipeServerWithCallback.WaitForConnectionAsync();

				string concatResult = await pipeServerWithCallback.InvokeAsync(c => c.Concatenate("a", "b"));
				Console.WriteLine("Concatenate result: " + concatResult);

				await pipeServerWithCallback.WaitForRemotePipeCloseAsync();

				Console.WriteLine("Client disconnected.");
			}
			catch (Exception exception)
			{
				Console.WriteLine("Exception in pipe processing: " + exception);
			}
		}
	}
}
