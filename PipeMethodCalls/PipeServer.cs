using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	/// <summary>
	/// A named pipe server.
	/// </summary>
	/// <typeparam name="THandling">The interface for requests that this server will be handling.</typeparam>
	public class PipeServer<THandling> : IPipeServer, IDisposable
		where THandling : class
	{
		private readonly string pipeName;
		private readonly Func<THandling> handlerFactoryFunc;
		private readonly PipeOptions? pipeOptions;
		private NamedPipeServerStream rawPipeStream;
		private PipeStreamWrapper wrappedPipeStream;
		private CancellationTokenSource workLoopCancellationTokenSource;
		private TaskCompletionSource<object> pipeCloseCompletionSource;
		private Action<string> logger;
		private Exception pipeFault;

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeServer"/> class.
		/// </summary>
		/// <param name="pipeName">The pipe name.</param>
		/// <param name="handlerFactoryFunc">A factory function to provide the handler implementation.</param>
		/// <param name="pipeOptions">Extra options for the pipe.</param>
		public PipeServer(string pipeName, Func<THandling> handlerFactoryFunc, PipeOptions? pipeOptions = null)
		{
			this.pipeName = pipeName;
			this.handlerFactoryFunc = handlerFactoryFunc;
			this.pipeOptions = pipeOptions;
		}

		/// <summary>
		/// Gets the state of the pipe.
		/// </summary>
		public PipeState State { get; private set; } = PipeState.NotOpened;

		/// <summary>
		/// Sets up the given action as a logger for the module.
		/// </summary>
		/// <param name="logger">The logger action.</param>
		public void SetLogger(Action<string> logger)
		{
			this.logger = logger;
		}

		/// <summary>
		/// Waits for a client to connect to the pipe.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		public async Task WaitForConnectionAsync(CancellationToken cancellationToken = default)
		{
			if (this.State != PipeState.NotOpened)
			{
				throw new InvalidOperationException("Can only call WaitForConnectionAsync once");
			}

			PipeOptions pipeOptionsToPass;
			if (this.pipeOptions == null)
			{
				pipeOptionsToPass = PipeOptions.Asynchronous;
			}
			else
			{
				pipeOptionsToPass = this.pipeOptions.Value | PipeOptions.Asynchronous;
			}

			this.rawPipeStream = new NamedPipeServerStream(this.pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, pipeOptionsToPass);
			this.rawPipeStream.ReadMode = PipeTransmissionMode.Message;

			this.logger.Log(() => $"Set up named pipe server '{this.pipeName}'.");

			await this.rawPipeStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

			this.logger.Log(() => "Connected to client.");

			this.wrappedPipeStream = new PipeStreamWrapper(this.rawPipeStream, this.logger);
			var requestHandler = new RequestHandler<THandling>(this.wrappedPipeStream, this.handlerFactoryFunc);

			this.State = PipeState.Connected;

			this.StartProcessing();
		}

		/// <summary>
		/// Wait for the other end to close the pipe.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <exception cref="PipeFaultedException">Thrown when the pipe has closed due to an unknown error.</exception>
		/// <remarks>This does not throw when the other end closes the pipe.</remarks>
		public Task WaitForRemotePipeCloseAsync(CancellationToken cancellationToken = default)
		{
			if (this.State == PipeState.Closed)
			{
				return Task.CompletedTask;
			}

			if (this.State == PipeState.Faulted)
			{
				return Task.FromException(this.pipeFault);
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

		/// <summary>
		/// Starts the processing loop on the pipe.
		/// </summary>
		private async void StartProcessing()
		{
			try
			{
				this.workLoopCancellationTokenSource = new CancellationTokenSource();

				// Process messages until canceled.
				while (true)
				{
					this.workLoopCancellationTokenSource.Token.ThrowIfCancellationRequested();
					await this.wrappedPipeStream.ProcessMessageAsync(this.workLoopCancellationTokenSource.Token).ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException)
			{
				// This is a normal dispose.
				this.State = PipeState.Closed;
				if (this.pipeCloseCompletionSource != null)
				{
					this.pipeCloseCompletionSource.TrySetResult(null);
				}
			}
			catch (Exception exception)
			{
				this.State = PipeState.Faulted;
				this.pipeFault = exception;
				if (this.pipeCloseCompletionSource != null)
				{
					this.pipeCloseCompletionSource.TrySetException(exception);
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

		/// <summary>
		/// Closes the pipe.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
		}
		#endregion
	}
}
