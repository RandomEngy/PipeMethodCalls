using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	public class PipeServer<THandling> : IPipeServer, IDisposable
	{
		private readonly string name;
		private readonly Func<THandling> handlerFactoryFunc;
		private NamedPipeServerStream rawPipeStream;
		private PipeStreamWrapper wrappedPipeStream;
		private CancellationTokenSource workLoopCancellationTokenSource;
		private TaskCompletionSource<object> pipeCloseCompletionSource;
		private Action<string> logger;
		private bool remotePipeOpen;

		public PipeServer(string name, Func<THandling> handlerFactoryFunc)
		{
			this.name = name;
			this.handlerFactoryFunc = handlerFactoryFunc;
		}

		public void SetLogger(Action<string> logger)
		{
			this.logger = logger;
		}

		public async Task WaitForConnectionAsync(CancellationToken cancellationToken = default)
		{
			if (this.remotePipeOpen)
			{
				throw new InvalidOperationException("Pipe is already connected.");
			}

			bool firstConnection = this.rawPipeStream == null;

			if (firstConnection)
			{
				this.rawPipeStream = new NamedPipeServerStream(this.name, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
				this.rawPipeStream.ReadMode = PipeTransmissionMode.Message;
			}

			this.logger.Log(() => $"Set up named pipe server '{this.name}'.");

			await this.rawPipeStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
			this.remotePipeOpen = true;

			this.logger.Log(() => "Connected to client.");

			if (firstConnection)
			{
				this.wrappedPipeStream = new PipeStreamWrapper(this.rawPipeStream, this.logger);
				var requestHandler = new RequestHandler<THandling>(this.wrappedPipeStream, this.handlerFactoryFunc);
			}

			this.StartProcessing();
		}

		/// <summary>
		/// Wait for the other end to close the pipe.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		public Task WaitForRemotePipeCloseAsync(CancellationToken cancellationToken = default)
		{
			if (!this.remotePipeOpen)
			{
				return Task.CompletedTask;
			}

			if (this.pipeCloseCompletionSource == null)
			{
				this.pipeCloseCompletionSource = new TaskCompletionSource<object>();
			}

			cancellationToken.Register(() =>
			{
				this.pipeCloseCompletionSource.SetCanceled();
			});

			return this.pipeCloseCompletionSource.Task;
		}

		private async void StartProcessing()
		{
			try
			{
				this.workLoopCancellationTokenSource = new CancellationTokenSource();

				// Process messages until canceled.
				while (!this.workLoopCancellationTokenSource.IsCancellationRequested)
				{
					await this.wrappedPipeStream.ProcessMessageAsync(this.workLoopCancellationTokenSource.Token).ConfigureAwait(false);
				}
			}
			catch (Exception)
			{
				// Cancel or close will fall in here
			}
			finally
			{
				this.remotePipeOpen = false;
				if (this.pipeCloseCompletionSource != null)
				{
					this.pipeCloseCompletionSource.SetResult(null);
				}
			}
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
