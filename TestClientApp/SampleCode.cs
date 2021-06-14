using PipeMethodCalls;
using PipeMethodCalls.NetJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestAppCore;

namespace TestClientApp
{
	public class SampleCode
	{
		public async Task RunAsync()
		{
			var pipeClient = new PipeClient<IAdder>(new NetJsonPipeSerializer(), "mypipe");
			await pipeClient.ConnectAsync();
			int result = await pipeClient.InvokeAsync(adder => adder.AddNumbers(1, 3));
		}
	}
}
