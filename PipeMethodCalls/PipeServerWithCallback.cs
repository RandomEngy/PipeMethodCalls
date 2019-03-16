using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	/// <summary>
	/// A named pipe server with a callback channel.
	/// </summary>
	/// <typeparam name="TRequesting">The callback channel interface that the client will be handling.</typeparam>
	/// <typeparam name="THandling">The interface for requests that this server will be handling.</typeparam>
	public class PipeServerWithCallback<TRequesting, THandling> : IDisposable, IPipeServerWithCallback<TRequesting>
		where TRequesting : class
		where THandling : class
	{
		private readonly string pipeName;
		private readonly Func<THandling> handlerFactoryFunc;
		private MethodInvoker<TRequesting> invoker;
		private NamedPipeServerStream rawPipeStream;
		private PipeStreamWrapper wrappedPipeStream;
		private CancellationTokenSource workLoopCancellationTokenSource;
		private TaskCompletionSource<object> pipeCloseCompletionSource;
		private Action<string> logger;
		private bool remotePipeOpen;

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeServerWithCallback"/> class.
		/// </summary>
		/// <param name="pipeName">The pipe name.</param>
		/// <param name="handlerFactoryFunc">A factory function to provide the handler implementation.</param>
		public PipeServerWithCallback(string pipeName, Func<THandling> handlerFactoryFunc)
		{
			this.pipeName = pipeName;
			this.handlerFactoryFunc = handlerFactoryFunc;
		}

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
			if (this.rawPipeStream != null)
			{
				throw new InvalidOperationException("Can only call WaitForConnectionAsync once");
			}

			this.rawPipeStream = new NamedPipeServerStream(this.pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
			this.rawPipeStream.ReadMode = PipeTransmissionMode.Message;

			this.logger.Log(() => $"Set up named pipe server '{this.pipeName}'.");

			await this.rawPipeStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
			this.remotePipeOpen = true;

			this.logger.Log(() => "Connected to client.");

			this.wrappedPipeStream = new PipeStreamWrapper(this.rawPipeStream, this.logger);
			this.invoker = new MethodInvoker<TRequesting>(this.wrappedPipeStream);
			var requestHandler = new RequestHandler<THandling>(this.wrappedPipeStream, this.handlerFactoryFunc);

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

		/// <summary>
		/// Starts the processing loop on the pipe.
		/// </summary>
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

		/// <summary>
		/// Invokes a method on the remote endpoint.
		/// </summary>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		public Task InvokeAsync(Expression<Action<TRequesting>> expression, CancellationToken cancellationToken = default)
		{
			Utilities.CheckInvoker(this.invoker);
			return this.invoker.InvokeAsync(expression, cancellationToken);
		}

		/// <summary>
		/// Invokes a method on the remote endpoint.
		/// </summary>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		public Task InvokeAsync(Expression<Func<TRequesting, Task>> expression, CancellationToken cancellationToken = default)
		{
			Utilities.CheckInvoker(this.invoker);
			return this.invoker.InvokeAsync(expression, cancellationToken);
		}

		/// <summary>
		/// Invokes a method on the remote endpoint.
		/// </summary>
		/// <typeparam name="TResult">The type of result from the method.</typeparam>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <returns>The method result.</returns>
		public Task<TResult> InvokeAsync<TResult>(Expression<Func<TRequesting, TResult>> expression, CancellationToken cancellationToken = default)
		{
			Utilities.CheckInvoker(this.invoker);
			return this.invoker.InvokeAsync(expression, cancellationToken);
		}

		/// <summary>
		/// Invokes a method on the remote endpoint.
		/// </summary>
		/// <typeparam name="TResult">The type of result from the method.</typeparam>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <returns>The method result.</returns>
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
