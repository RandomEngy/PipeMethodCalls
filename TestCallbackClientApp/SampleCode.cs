using PipeMethodCalls;
using PipeMethodCalls.NetJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestAppCore;

namespace TestCallbackClientApp
{
	public class SampleCode
	{
		public async Task RunAsync()
		{
			var pipeClient = new PipeClientWithCallback<IAdder, IConcatenator>(
				new NetJsonPipeSerializer(),
				"mypipe",
				() => new Concatenator());

			await pipeClient.ConnectAsync();

			int result = await pipeClient.InvokeAsync(adder => adder.AddNumbers(4, 7));
		}
	}
}
