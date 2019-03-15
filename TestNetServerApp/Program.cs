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
			var pipeServerWithCallback = new PipeServerWithCallback<IConcatenator, IAdder>("testpipe", () => new Adder());
			pipeServerWithCallback.SetLogger(message => Console.WriteLine(message));
			CancellationTokenSource cSource = new CancellationTokenSource();

			Task task = pipeServerWithCallback.ConnectAsync(cSource.Token);

			Console.ReadKey();
			cSource.Cancel();
		}
	}
}
