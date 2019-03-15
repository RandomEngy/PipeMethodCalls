using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	public class PipeClient<TRequesting>
	{
		private readonly string name;
		private readonly string machine;
		private MethodInvoker<TRequesting> invoker;

		public PipeClient(string name, string machine = null)
		{
			this.name = name;
			this.machine = machine;
		}

		public async Task RunAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			//NamedPipeClientStream rawPipeStream;
			//if (this.machine == null)
			//{
			//	rawPipeStream = new NamedPipeClientStream(".", this.name, PipeDirection.InOut, PipeOptions.Asynchronous);
			//}
			//else
			//{
			//	rawPipeStream = new NamedPipeClientStream(this.machine, this.name, PipeDirection.InOut, PipeOptions.Asynchronous);
			//}

			//rawPipeStream.ReadMode = PipeTransmissionMode.Message;

			//using (rawPipeStream)
			//{
			//	await rawPipeStream.ConnectAsync(cancellationToken).ConfigureAwait(false);

			//	var wrappedPipeStream = new PipeStreamWrapper(rawPipeStream);
			//	this.invoker = new MethodInvoker<TRequesting>(wrappedPipeStream);

			//	// Process messages until canceled.
			//	while (!cancellationToken.IsCancellationRequested)
			//	{
			//		await wrappedPipeStream.ProcessMessageAsync(cancellationToken).ConfigureAwait(false);
			//	}
			//}
		}

		public Task InvokeAsync(Expression<Action<TRequesting>> expression, CancellationToken cancellationToken = default(CancellationToken))
		{
			Utilities.CheckInvoker(this.invoker);
			return this.invoker.InvokeAsync(expression, cancellationToken);
		}

		public Task<TResult> InvokeAsync<TResult>(Expression<Func<TRequesting, TResult>> expression, CancellationToken cancellationToken = default(CancellationToken))
		{
			Utilities.CheckInvoker(this.invoker);
			return this.invoker.InvokeAsync(expression, cancellationToken);
		}

		public Task InvokeAsync(Expression<Func<TRequesting, Task>> expression, CancellationToken cancellationToken = default(CancellationToken))
		{
			Utilities.CheckInvoker(this.invoker);
			return this.invoker.InvokeAsync(expression, cancellationToken);
		}

		public Task<TResult> InvokeAsync<TResult>(Expression<Func<TRequesting, Task<TResult>>> expression, CancellationToken cancellationToken = default(CancellationToken))
		{
			Utilities.CheckInvoker(this.invoker);
			return this.invoker.InvokeAsync(expression, cancellationToken);
		}
	}
}
