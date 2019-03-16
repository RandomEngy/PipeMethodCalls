using PipeMethodCalls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestAppCore;

namespace TestNetServerApp
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
			var pipeServerWithCallback = new PipeServerWithCallback<IConcatenator, IAdder>("testpipe", () => new Adder());
			pipeServerWithCallback.SetLogger(message => Console.WriteLine(message));

			await pipeServerWithCallback.WaitForConnectionAsync();

			await pipeServerWithCallback.WaitForRemotePipeCloseAsync();

			Console.WriteLine("Client disconnected.");
		}
	}
}
