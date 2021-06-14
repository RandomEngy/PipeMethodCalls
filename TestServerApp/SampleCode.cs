using PipeMethodCalls;
using PipeMethodCalls.NetJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestAppCore;

namespace TestServerApp
{
	public class SampleCode
	{
		public async Task RunAsync()
		{
			var pipeServer = new PipeServer<IAdder>(
				new NetJsonPipeSerializer(),
				"mypipe",
				() => new Adder());
			
			await pipeServer.WaitForConnectionAsync();
		}
	}
}
