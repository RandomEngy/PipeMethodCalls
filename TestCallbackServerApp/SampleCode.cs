using PipeMethodCalls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestAppCore;

namespace TestCallbackServerApp
{
	public class SampleCode
	{
		public async Task RunAsync()
		{
			var pipeServer = new PipeServerWithCallback<IConcatenator, IAdder>("testpipe", () => new Adder());
			await pipeServer.WaitForConnectionAsync();
			string concatResult = await pipeServer.InvokeAsync(c => c.Concatenate("a", "b"));
		}
	}
}
