using PipeMethodCalls;
using PipeMethodCalls.NetJson;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestAppCore;

namespace TestServerApp
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
			var rawPipeStream = new NamedPipeServerStream("mypipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
			var pipeServer = new PipeServer<IAdder>(new NetJsonPipeSerializer(), rawPipeStream, () => new Adder());

			//var pipeServer = new PipeServer<IAdder>(new NetJsonPipeSerializer(), "mypipe", () => new Adder());
			pipeServer.SetLogger(message => Console.WriteLine(message));

			try
			{
				await pipeServer.WaitForConnectionAsync();

				await pipeServer.WaitForRemotePipeCloseAsync();

				Console.WriteLine("Client disconnected.");
			}
			catch (Exception exception)
			{
				Console.WriteLine("Exception in pipe processing: " + exception);
			}
		}
	}
}
