using PipeMethodCalls;
using System;
using System.Collections.Generic;
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
			var pipeServerWithCallback = new PipeServer<IAdder>("testpipe", () => new Adder());
			pipeServerWithCallback.SetLogger(message => Console.WriteLine(message));

			try
			{
				await pipeServerWithCallback.WaitForConnectionAsync();

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
