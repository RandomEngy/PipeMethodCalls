using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	public class PipeClientWithCallback<TRequesting, THandling> : IDisposable
	{
		private readonly string name;
		private readonly string machine;
		private MethodInvoker<TRequesting> invoker;
		private NamedPipeClientStream rawPipeStream;
		private PipeStreamWrapper wrappedPipeStream;
		private CancellationTokenSource workLoopCancellationTokenSource;

		public PipeClientWithCallback(string name, string machine = null)
		{
			this.name = name;
			this.machine = machine;
		}

		public async Task ConnectAsync(Func<THandling> handlerFunc, CancellationToken cancellationToken = default)
		{
			if (this.machine == null)
			{
				this.rawPipeStream = new NamedPipeClientStream(".", this.name, PipeDirection.InOut, PipeOptions.Asynchronous);
			}
			else
			{
				this.rawPipeStream = new NamedPipeClientStream(this.machine, this.name, PipeDirection.InOut, PipeOptions.Asynchronous);
			}

			await this.rawPipeStream.ConnectAsync(cancellationToken).ConfigureAwait(false);
			this.rawPipeStream.ReadMode = PipeTransmissionMode.Message;

			this.wrappedPipeStream = new PipeStreamWrapper(this.rawPipeStream);
			this.invoker = new MethodInvoker<TRequesting>(wrappedPipeStream);
			var requestHandler = new RequestHandler<THandling>(this.wrappedPipeStream, handlerFunc);

			this.StartProcessing();
		}

		public async void StartProcessing()
		{
			this.workLoopCancellationTokenSource = new CancellationTokenSource();

			// Process messages until canceled.
			while (!this.workLoopCancellationTokenSource.IsCancellationRequested)
			{
				await this.wrappedPipeStream.ProcessMessageAsync(this.workLoopCancellationTokenSource.Token).ConfigureAwait(false);
			}
		}

		public Task InvokeAsync(Expression<Action<TRequesting>> expression, CancellationToken cancellationToken = default)
		{
			Utilities.CheckInvoker(this.invoker);
			return this.invoker.InvokeAsync(expression, cancellationToken);
		}

		public Task<TResult> InvokeAsync<TResult>(Expression<Func<TRequesting, TResult>> expression, CancellationToken cancellationToken = default)
		{
			Utilities.CheckInvoker(this.invoker);
			return this.invoker.InvokeAsync(expression, cancellationToken);
		}

		public Task InvokeAsync(Expression<Func<TRequesting, Task>> expression, CancellationToken cancellationToken = default)
		{
			Utilities.CheckInvoker(this.invoker);
			return this.invoker.InvokeAsync(expression, cancellationToken);
		}

		public Task<TResult> InvokeAsync<TResult>(Expression<Func<TRequesting, Task<TResult>>> expression, CancellationToken cancellationToken = default)
		{
			Utilities.CheckInvoker(this.invoker);
			return this.invoker.InvokeAsync(expression, cancellationToken);
		}

		#region IDisposable Support
		private bool disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					this.workLoopCancellationTokenSource.Cancel();
					this.invoker = null;

					if (this.rawPipeStream != null)
					{
						this.rawPipeStream.Dispose();
					}
				}

				this.disposed = true;
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
		}
		#endregion
	}
}
