using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// Creates a pool of servers with callbacks.
	/// </summary>
	/// <typeparam name="THandling">The interface for requests that this server will be handling.</typeparam>
	public class PipeServerPool : IDisposable
	{
		private readonly Func<IPipeServer> serverFactoryFunc;
		private readonly int serverCount;
		private readonly object serverLock = new object();
		private readonly List<IPipeServer> servers = new List<IPipeServer>();
		private Action<string> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeServerWithCallbackPool" /> class.
		/// </summary>
		/// <param name="serverFactoryFunc">A factory function to provide the handler implementation.</param>
		/// <param name="serverCount">The total numbers of servers running.</param>
		public PipeServerPool(Func<IPipeServer> serverFactoryFunc, int serverCount)
		{
			this.serverFactoryFunc = serverFactoryFunc;
			this.serverCount = serverCount;
		}

		/// <summary>
		/// Starts the pool.
		/// </summary>
		public void Start()
		{
			if (this.disposed)
			{
				throw new InvalidOperationException("Cannot start a pool after it has been disposed.");
			}

			this.AddServers();
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
		/// Starts processing for the given server.
		/// </summary>
		/// <param name="server">The server to start processing for.</param>
		private async void StartServer(IPipeServer server)
		{
			this.servers.Add(server);

			try
			{
				await server.WaitForConnectionAsync().ConfigureAwait(false);
				await server.WaitForRemotePipeCloseAsync().ConfigureAwait(false);
			}
			catch (Exception)
			{
			}
			finally
			{
				this.servers.Remove(server);

				if (!this.disposed)
				{
					this.AddServers();
				}
			}
		}

		/// <summary>
		/// Adds servers until the count is reached.
		/// </summary>
		private void AddServers()
		{
			lock (this.serverLock)
			{
				while (this.servers.Count < serverCount)
				{
					var server = this.serverFactoryFunc();
					if (this.logger != null)
					{
						server.SetLogger(this.logger);
					}

					this.StartServer(server);
					this.servers.Add(server);
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
					lock (this.serverLock)
					{
						foreach (var server in this.servers)
						{
							server.Dispose();
						}
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
