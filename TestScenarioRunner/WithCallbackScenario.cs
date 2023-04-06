using PipeMethodCalls;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore;

namespace TestScenarioRunner
{
    public static class WithCallbackScenario
    {
        public static async Task RunClientAsync(PipeClientWithCallback<IAdder, IConcatenator> pipeClientWithCallback)
        {
			await pipeClientWithCallback.ConnectAsync().ConfigureAwait(false);
			WrappedInt result = await pipeClientWithCallback.InvokeAsync(adder => adder.AddWrappedNumbers(new WrappedInt { Num = 1 }, new WrappedInt { Num = 3 })).ConfigureAwait(false);
			result.Num.ShouldBe(4);

			int asyncResult = await pipeClientWithCallback.InvokeAsync(adder => adder.AddAsync(4, 7)).ConfigureAwait(false);
			asyncResult.ShouldBe(11);

			IList<string> listifyResult = await pipeClientWithCallback.InvokeAsync(adder => adder.Listify("item")).ConfigureAwait(false);
			listifyResult.Count.ShouldBe(1);
			listifyResult[0].ShouldBe("item");

			int unwrapResult = await pipeClientWithCallback.InvokeAsync(adder => adder.Unwrap(null)).ConfigureAwait(false);
			unwrapResult.ShouldBe(0);

			await pipeClientWithCallback.InvokeAsync(adder => adder.DoesNothing()).ConfigureAwait(false);
			await pipeClientWithCallback.InvokeAsync(adder => adder.DoesNothingAsync()).ConfigureAwait(false);

			var expectedException = await Should.ThrowAsync<PipeInvokeFailedException>(async () =>
			{
				await pipeClientWithCallback.InvokeAsync(adder => adder.AlwaysFails()).ConfigureAwait(false);
			});

			expectedException.Message.ShouldContain("This method always fails");

			var refException = await Should.ThrowAsync<PipeInvokeFailedException>(async () =>
			{
				int refValue = 4;
				await pipeClientWithCallback.InvokeAsync(adder => adder.HasRefParam(ref refValue)).ConfigureAwait(false);
			});

			refException.Message.ShouldBe("ref parameters are not supported. Method: 'HasRefParam'");
			pipeClientWithCallback.Dispose();
		}

		public static async Task RunServerAsync(PipeServerWithCallback<IConcatenator, IAdder> pipeServerWithCallback)
        {
			await pipeServerWithCallback.WaitForConnectionAsync().ConfigureAwait(false);

			string concatResult = await pipeServerWithCallback.InvokeAsync(c => c.Concatenate("a", "b")).ConfigureAwait(false);
			concatResult.ShouldBe("ab");

			// 100 character string. Concat to make sure that the continuation bit works for varint.
			string longString = "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789";

			string longConcatResult = await pipeServerWithCallback.InvokeAsync(c => c.Concatenate(longString, longString)).ConfigureAwait(false);
			longConcatResult.ShouldBe(longString + longString);

			await pipeServerWithCallback.WaitForRemotePipeCloseAsync().ConfigureAwait(false);
		}
    }
}
