using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	public class PipeServer<THandling>
	{
		private readonly string name;

		public PipeServer(string name)
		{
			this.name = name;
		}

		public async Task ConnectAsync(Func<THandling> handlerFunc, CancellationToken cancellationToken = default(CancellationToken))
		{
			using (var rawPipeStream = new NamedPipeServerStream(this.name, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous))
			{
				await rawPipeStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
				rawPipeStream.ReadMode = PipeTransmissionMode.Message;

				var wrappedPipeStream = new PipeStreamWrapper(rawPipeStream);
				var requestHandler = new RequestHandler<THandling>(wrappedPipeStream, handlerFunc);

				// Process messages until canceled.
				while (!cancellationToken.IsCancellationRequested)
				{
					await wrappedPipeStream.ProcessMessageAsync(cancellationToken).ConfigureAwait(false);
				}
			}
		}
	}
}
