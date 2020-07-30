﻿using PipeMethodCalls;
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
			var pipeClient = new PipeClient<IAdder>("mypipe");
			pipeClient.SetLogger(message => Console.WriteLine(message));

			try
			{
				await pipeClient.ConnectAsync().ConfigureAwait(false);
				WrappedInt result = await pipeClient.InvokeAsync(adder => adder.AddWrappedNumbers(new WrappedInt { Num = 1 }, new WrappedInt { Num = 3 })).ConfigureAwait(false);

				Console.WriteLine("Server add result: " + result.Num);

				await pipeClient.WaitForRemotePipeCloseAsync();

				Console.WriteLine("Server closed pipe.");
			}
			catch (Exception exception)
			{
				Console.WriteLine("Exception in pipe processing: " + exception);
			}
		}
	}
}
