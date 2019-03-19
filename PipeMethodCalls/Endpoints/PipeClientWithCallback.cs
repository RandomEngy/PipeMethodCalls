using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	/// <summary>
	/// A named pipe client with a callback channel.
	/// </summary>
	/// <typeparam name="TRequesting">The interface that the client will be invoking on the server.</typeparam>
	/// <typeparam name="THandling">The callback channel interface that this client will be handling.</typeparam>
	public class PipeClientWithCallback<TRequesting, THandling> : IDisposable, IPipeClient<TRequesting>
		where TRequesting : class
		where THandling : class
	{
		private readonly string pipeName;
		private readonly string serverName;
		private readonly Func<THandling> handlerFactoryFunc;
		private readonly PipeOptions? options;
		private readonly TokenImpersonationLevel? impersonationLevel;
		private readonly HandleInheritability? inheritability;
		private NamedPipeClientStream rawPipeStream;
		private PipeStreamWrapper wrappedPipeStream;
		private Action<string> logger;
		private PipeMessageProcessor messageProcessor = new PipeMessageProcessor();

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeClientWithCallback"/> class.
		/// </summary>
		/// <param name="pipeName">The name of the pipe.</param>
		/// <param name="handlerFactoryFunc">A factory function to provide the handler implementation.</param>
		public PipeClientWithCallback(string pipeName, Func<THandling> handlerFactoryFunc)
			: this(".", pipeName, handlerFactoryFunc)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeClientWithCallback"/> class.
		/// </summary>
		/// <param name="pipeName">The name of the pipe.</param>
		/// <param name="handlerFactoryFunc">A factory function to provide the handler implementation.</param>
		/// <param name="options">One of the enumeration values that determines how to open or create the pipe.</param>
		/// <param name="impersonationLevel">One of the enumeration values that determines the security impersonation level.</param>
		/// <param name="inheritability">One of the enumeration values that determines whether the underlying handle will be inheritable by child processes.</param>
		public PipeClientWithCallback(string pipeName, Func<THandling> handlerFactoryFunc, PipeOptions options, TokenImpersonationLevel impersonationLevel, HandleInheritability inheritability)
			: this(".", pipeName, handlerFactoryFunc, options, impersonationLevel, inheritability)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeClientWithCallback"/> class.
		/// </summary>
		/// <param name="serverName">The name of the server to connect to.</param>
		/// <param name="pipeName">The name of the pipe.</param>
		/// <param name="handlerFactoryFunc">A factory function to provide the handler implementation.</param>
		public PipeClientWithCallback(string serverName, string pipeName, Func<THandling> handlerFactoryFunc)
		{
			this.pipeName = pipeName;
			this.serverName = serverName;
			this.handlerFactoryFunc = handlerFactoryFunc;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeClientWithCallback"/> class.
		/// </summary>
		/// <param name="serverName">The name of the server to connect to.</param>
		/// <param name="pipeName">The name of the pipe.</param>
		/// <param name="handlerFactoryFunc">A factory function to provide the handler implementation.</param>
		/// <param name="options">One of the enumeration values that determines how to open or create the pipe.</param>
		/// <param name="impersonationLevel">One of the enumeration values that determines the security impersonation level.</param>
		/// <param name="inheritability">One of the enumeration values that determines whether the underlying handle will be inheritable by child processes.</param>
		public PipeClientWithCallback(string serverName, string pipeName, Func<THandling> handlerFactoryFunc, PipeOptions options, TokenImpersonationLevel impersonationLevel, HandleInheritability inheritability)
		{
			this.pipeName = pipeName;
			this.serverName = serverName;
			this.handlerFactoryFunc = handlerFactoryFunc;
			this.options = options;
			this.impersonationLevel = impersonationLevel;
			this.inheritability = inheritability;
		}

		/// <summary>
		/// Gets the state of the pipe.
		/// </summary>
		public PipeState State => this.messageProcessor.State;

		/// <summary>
		/// Gets the method invoker.
		/// </summary>
		/// <remarks>This is null before connecting.</remarks>
		public IPipeInvoker<TRequesting> Invoker { get; private set; }

		/// <summary>
		/// Sets up the given action as a logger for the module.
		/// </summary>
		/// <param name="logger">The logger action.</param>
		public void SetLogger(Action<string> logger)
		{
			this.logger = logger;
		}

		/// <summary>
		/// Connects the pipe to the server.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <exception cref="IOException">Thrown when the connection fails.</exception>
		public async Task ConnectAsync(CancellationToken cancellationToken = default)
		{
			if (this.State != PipeState.NotOpened)
			{
				throw new InvalidOperationException("Can only call ConnectAsync once");
			}

			this.logger.Log(() => $"Connecting to named pipe '{this.pipeName}' on machine '{this.serverName}'");

			if (this.options != null)
			{
				this.rawPipeStream = new NamedPipeClientStream(this.serverName, this.pipeName, PipeDirection.InOut, this.options.Value | PipeOptions.Asynchronous, this.impersonationLevel.Value, this.inheritability.Value);
			}
			else
			{
				this.rawPipeStream = new NamedPipeClientStream(this.serverName, this.pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
			}

			await this.rawPipeStream.ConnectAsync(cancellationToken).ConfigureAwait(false);
			this.logger.Log(() => "Connected.");

			this.rawPipeStream.ReadMode = PipeTransmissionMode.Message;

			this.wrappedPipeStream = new PipeStreamWrapper(this.rawPipeStream, this.logger);
			this.Invoker = new MethodInvoker<TRequesting>(this.wrappedPipeStream, this.messageProcessor);
			var requestHandler = new RequestHandler<THandling>(this.wrappedPipeStream, handlerFactoryFunc);

			this.messageProcessor.StartProcessing(wrappedPipeStream);
		}

		/// <summary>
		/// Wait for the other end to close the pipe.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <exception cref="IOException">Thrown when the pipe has closed due to an unknown error.</exception>
		/// <remarks>This does not throw when the other end closes the pipe.</remarks>
		public Task WaitForRemotePipeCloseAsync(CancellationToken cancellationToken = default)
		{
			return this.messageProcessor.WaitForRemotePipeCloseAsync(cancellationToken);
		}

		#region IDisposable Support
		private bool disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					this.messageProcessor.Dispose();

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
