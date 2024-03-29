﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	/// <summary>
	/// Wraps the raw pipe stream with messaging and request/response capability.
	/// </summary>
	internal class PipeStreamWrapper
	{
		private readonly PipeStream stream;
		private readonly Action<string> logger;

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
		public Task SendRequestAsync(SerializedPipeRequest request, CancellationToken cancellationToken)
		{
			return this.SendMessageAsync(MessageType.Request, request, cancellationToken);
		}

		/// <summary>
		/// Sends a response.
		/// </summary>
		/// <param name="response">The response to send.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		public Task SendResponseAsync(SerializedPipeResponse response, CancellationToken cancellationToken)
		{
			return this.SendMessageAsync(MessageType.Response, response, cancellationToken);
		}

		/// <summary>
		/// Sends a message.
		/// </summary>
		/// <param name="messageType">The type of message.</param>
		/// <param name="payloadObject">The massage payload object.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		private async Task SendMessageAsync(MessageType messageType, IMessage payloadObject, CancellationToken cancellationToken)
		{
			// We use a custom binary format to avoid reliance on a specific serialization technology.
			// Strings are UTF-8 null-terminated.
			//
			// # of Bytes - Description
			// varint - Payload length - number of bytes in message (excluding the payload length itself)
			// 1 - MessageType
			// varint - CallId
			// If Request
			//   string - MethodName
			//   varint - number of parameters
			//   Repeat N times
			//     varint - parameter length in bytes
			//     N - parameter bytes
			//   varint - generic arguments types length
			//   Repeat N times
			//     string - generic argument type
			// If Response
			//   1 - succeeded boolean
			//   If succeeded
			//     varint - result length in bytes
			//     N - result bytes
			//   If failed
			//     string - error

			using (var messageStream = new MemoryStream(35))
			{
				messageStream.WriteByte((byte)messageType);

				// Write the call ID
				messageStream.WriteVarInt(payloadObject.CallId);

				if (payloadObject.GetType() == typeof(SerializedPipeRequest))
				{
					SerializedPipeRequest request = (SerializedPipeRequest)payloadObject;
					messageStream.WriteUtf8String(request.MethodName);

					messageStream.WriteArray(request.Parameters);

					messageStream.WriteVarInt(request.GenericArguments.Length);
					foreach (Type genericArgument in request.GenericArguments)
					{
						messageStream.WriteUtf8String(genericArgument.ToString());
					}
				}
				else
				{
					SerializedPipeResponse response = (SerializedPipeResponse)payloadObject;

					messageStream.WriteByte(BitConverter.GetBytes(response.Succeeded)[0]);

					if (response.Succeeded)
					{
						messageStream.WriteVarInt(response.Data.Length);
						messageStream.Write(response.Data, 0, response.Data.Length);
					}
					else
					{
						messageStream.WriteUtf8String(response.Error);
					}
				}

				byte[] messageBytes = messageStream.ToArray();

				byte[] messageLengthBytes = Utilities.GetVarInt(messageBytes.Length);

				this.logger.Log(() => "Sending message bytes: 0x" + Utilities.BytesToHexString(messageLengthBytes) + Utilities.BytesToHexString(messageBytes));

				await this.writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
				try
				{
					// Write out the message length
					await this.stream.WriteAsync(messageLengthBytes, 0, messageLengthBytes.Length, cancellationToken).ConfigureAwait(false);

					// Write the message payload
					await this.stream.WriteAsync(messageBytes, 0, messageBytes.Length, cancellationToken).ConfigureAwait(false);
				}
				finally
				{
					this.writeLock.Release();
				}
			}
		}

		/// <summary>
		/// Processes the next message on the input stream.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		public async Task ProcessMessageAsync(CancellationToken cancellationToken)
		{
			var message = await this.ReadMessageAsync(cancellationToken).ConfigureAwait(false);

			switch (message.messageType)
			{
				case MessageType.Request:
					SerializedPipeRequest request = (SerializedPipeRequest)message.messageObject;

					if (this.RequestHandler == null)
					{
						throw new InvalidOperationException("Request received but this endpoint is not set up to handle requests.");
					}

					this.RequestHandler.HandleRequest(request);
					break;
				case MessageType.Response:
					SerializedPipeResponse response = (SerializedPipeResponse)message.messageObject;

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
		private async Task<(MessageType messageType, object messageObject)> ReadMessageAsync(CancellationToken cancellationToken)
		{
			// Read the length to see how long the message is
			int messagePayloadLength = await this.stream.ReadVarIntAsync(cancellationToken).ConfigureAwait(false);
			if (messagePayloadLength == 0)
			{
				this.ClosePipe();
			}

			this.logger.Log(() => "Reading message with length " + messagePayloadLength);

			byte[] messagePayloadBytes = new byte[messagePayloadLength];

			int payloadBytesRead = 0;
			while (payloadBytesRead < messagePayloadLength)
			{
				int readBytes = await this.stream.ReadAsync(messagePayloadBytes, payloadBytesRead, messagePayloadLength - payloadBytesRead, cancellationToken).ConfigureAwait(false);
				if (readBytes == 0)
				{
					this.ClosePipe();
				}

				payloadBytesRead += readBytes;
			}

			// We've read in the whole message. Now parse it out into a message object.
			using (MemoryStream messageStream = new MemoryStream(messagePayloadBytes))
			{
				var messageType = (MessageType)messageStream.ReadByte();
				int callId = messageStream.ReadVarInt();

				object messageObject;
				if (messageType == MessageType.Request)
				{
					string methodName = messageStream.ReadUtf8String();
					byte[][] parameters = messageStream.ReadArray();

					int genericArgumentCount = messageStream.ReadVarInt();
					Type[] genericArguments = new Type[genericArgumentCount];
					for (int i = 0; i < genericArgumentCount; i++)
					{
						string genericArgumentString = messageStream.ReadUtf8String();
						genericArguments[i] = Type.GetType(genericArgumentString);
					}

					messageObject = new SerializedPipeRequest { CallId = callId, MethodName = methodName, Parameters = parameters, GenericArguments = genericArguments };
				}
				else
				{
					// Response
					bool success = BitConverter.ToBoolean(new byte[] { (byte)messageStream.ReadByte() }, 0);
					if (success)
					{
						int resultPayloadLength = messageStream.ReadVarInt();
						byte[] resultBytes = new byte[resultPayloadLength];
						messageStream.Read(resultBytes, 0, resultPayloadLength);

						messageObject = SerializedPipeResponse.Success(callId, resultBytes);
					}
					else
					{
						string error = messageStream.ReadUtf8String();
						messageObject = SerializedPipeResponse.Failure(callId, error);
					}
				}

				return (messageType, messageObject);
			}
		}

		/// <summary>
		/// Logs that the pipe has closed and throws exception to triggure graceful closure.
		/// </summary>
		private void ClosePipe()
		{
			string message = "Pipe has closed.";
			this.logger.Log(() => message);

			// OperationCanceledException is handled as pipe closing gracefully.
			throw new OperationCanceledException(message);
		}
	}
}
