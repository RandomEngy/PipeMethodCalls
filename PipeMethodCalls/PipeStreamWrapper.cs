using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PipeMethodCalls
{
	/// <summary>
	/// Wraps the raw pipe stream with messaging and request/response capability.
	/// </summary>
	internal class PipeStreamWrapper
	{
		private readonly byte[] readBuffer = new byte[1024];
		private readonly PipeStream stream;
		private readonly Action<string> logger;
		private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore
		};

		// Prevents more than one thread from writing to the pipe stream at once
		private readonly SemaphoreSlim writeLock = new SemaphoreSlim(1, 1);

		/// <summary>
		/// Initializes a new instance of the <see cref="PipeStreamWrapper"/> class.
		/// </summary>
		/// <param name="stream">The raw pipe stream to wrap.</param>
		/// <param name="logger">The action to run to log events.</param>
		public PipeStreamWrapper(PipeStream stream, Action<string> logger)
		{
			this.stream = stream;
			this.logger = logger;
		}

		/// <summary>
		/// Gets or sets the registered request handler.
		/// </summary>
		public IRequestHandler RequestHandler { get; set; }

		/// <summary>
		/// Gets or sets the registered response handler.
		/// </summary>
		public IResponseHandler ResponseHandler { get; set; }

		/// <summary>
		/// Sends a request.
		/// </summary>
		/// <param name="request">The request to send.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		public Task SendRequestAsync(PipeRequest request, CancellationToken cancellationToken)
		{
			return this.SendMessageAsync(MessageType.Request, request, cancellationToken);
		}

		/// <summary>
		/// Sends a response.
		/// </summary>
		/// <param name="response">The response to send.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		public Task SendResponseAsync(PipeResponse response, CancellationToken cancellationToken)
		{
			return this.SendMessageAsync(MessageType.Response, response, cancellationToken);
		}

		/// <summary>
		/// Sends a message.
		/// </summary>
		/// <param name="messageType">The type of message.</param>
		/// <param name="payloadObject">The massage payload object.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		private async Task SendMessageAsync(MessageType messageType, object payloadObject, CancellationToken cancellationToken)
		{
			string payloadJson = JsonConvert.SerializeObject(payloadObject, serializerSettings);
			this.logger.Log(() => $"Sending {messageType} message" + Environment.NewLine + payloadJson);
			byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

			int payloadLength = payloadBytes.Length;

			// First byte is the message type
			byte[] messageBytes = new byte[payloadLength + 5];
			messageBytes[0] = (byte)messageType;

			// Next 4 bytes is the payload length
			byte[] payloadLengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payloadLength));
			payloadLengthBytes.CopyTo(messageBytes, 1);

			// Rest is the payload
			payloadBytes.CopyTo(messageBytes, 5);

			await this.writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				await this.stream.WriteAsync(messageBytes, 0, messageBytes.Length, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				this.writeLock.Release();
			}
		}

		/// <summary>
		/// Processes the next message on the input stream.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		public async Task ProcessMessageAsync(CancellationToken cancellationToken)
		{
			var message = await this.ReadMessageAsync(cancellationToken).ConfigureAwait(false);
			string json = message.jsonPayload;

			switch (message.messageType)
			{
				case MessageType.Request:
					this.logger.Log(() => "Handling request" + Environment.NewLine + json);
					PipeRequest request = JsonConvert.DeserializeObject<PipeRequest>(json, serializerSettings);

					if (this.RequestHandler == null)
					{
						throw new InvalidOperationException("Request received but this endpoint is not set up to handle requests.");
					}

					this.RequestHandler.HandleRequest(request);
					break;
				case MessageType.Response:
					this.logger.Log(() => "Handling response" + Environment.NewLine + json);
					PipeResponse response = JsonConvert.DeserializeObject<PipeResponse>(json, serializerSettings);

					if (this.ResponseHandler == null)
					{
						throw new InvalidOperationException("Response received but this endpoint is not set up to make requests.");
					}

					this.ResponseHandler.HandleResponse(response);
					break;
				default:
					throw new InvalidOperationException($"Unrecognized message type: {message.messageType}");
			}
		}

		/// <summary>
		/// Reads the message off the input stream.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>The read message type and payload.</returns>
		private async Task<(MessageType messageType, string jsonPayload)> ReadMessageAsync(CancellationToken cancellationToken)
		{
			// Read the 5-byte header to see the message type and how long the message is
			await this.stream.ReadAsync(this.readBuffer, 0, 5).ConfigureAwait(false);

			var messageType = (MessageType)this.readBuffer[0];
			int payloadLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(this.readBuffer, 1));

			byte[] payloadBytes = new byte[payloadLength];

			int payloadBytesRead = 0;
			using (var memoryStream = new MemoryStream())
			{
				while (payloadBytesRead < payloadLength)
				{
					int maxBytesToRead = Math.Min(this.readBuffer.Length, payloadLength - payloadBytesRead);

					var readBytes = await this.stream.ReadAsync(this.readBuffer, 0, maxBytesToRead, cancellationToken).ConfigureAwait(false);
					if (readBytes == 0)
					{
						string message = "Pipe has closed.";
						this.logger.Log(() => message);

						// OperationCanceledException is handled as pipe closing gracefully.
						throw new OperationCanceledException(message);
					}

					await memoryStream.WriteAsync(this.readBuffer, 0, readBytes, cancellationToken).ConfigureAwait(false);
					payloadBytesRead += readBytes;
				}

				payloadBytes = memoryStream.ToArray();
			}

			string jsonPayload = Encoding.UTF8.GetString(payloadBytes);

			return (messageType, jsonPayload);
		}
	}
}
