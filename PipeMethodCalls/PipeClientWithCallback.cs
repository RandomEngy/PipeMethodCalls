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
		private readonly Func<THandling> handlerFactoryFunc;
		private MethodInvoker<TRequesting> invoker;
		private NamedPipeClientStream rawPipeStream;
		private PipeStreamWrapper wrappedPipeStream;
		private CancellationTokenSource workLoopCancellationTokenSource;
		private Action<string> logger;

		public PipeClientWithCallback(string name, Func<THandling> handlerFactoryFunc)
		{
			this.name = name;
			this.machine = ".";
			this.handlerFactoryFunc = handlerFactoryFunc;
		}

		public PipeClientWithCallback(string machine, string name, Func<THandling> handlerFactoryFunc)
		{
			this.name = name;
			this.machine = machine;
			this.handlerFactoryFunc = handlerFactoryFunc;
		}

		public void SetLogger(Action<string> logger)
		{
			this.logger = logger;
		}

		public async Task ConnectAsync(CancellationToken cancellationToken = default)
		{
			this.logger.Log(() => $"Connecting to named pipe '{this.name}' on machine {this.machine}...");

			this.rawPipeStream = new NamedPipeClientStream(this.machine, this.name, PipeDirection.InOut, PipeOptions.Asynchronous);
			await this.rawPipeStream.ConnectAsync(cancellationToken).ConfigureAwait(false);
			this.logger.Log(() => "Connected.");

			this.rawPipeStream.ReadMode = PipeTransmissionMode.Message;

			this.wrappedPipeStream = new PipeStreamWrapper(this.rawPipeStream, this.logger);
			this.invoker = new MethodInvoker<TRequesting>(wrappedPipeStream);
			var requestHandler = new RequestHandler<THandling>(this.wrappedPipeStream, handlerFactoryFunc);

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
