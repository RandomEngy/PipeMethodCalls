using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// A response sent over the pipe that has been user serialized with an <see cref="IPipeSerializer"/>.
	/// </summary>
	/// <remarks>
	/// There are two stages to serialization:
	/// 1: User serialization, where typed values are serialized to a byte array.
	/// 2: Message serialization, where the message is sent with a custom binary serialization over the pipe.
	/// </remarks>
	internal class SerializedPipeResponse : IMessage
	{
		/// <summary>
		/// The call ID.
		/// </summary>
		public int CallId { get; private set; }

		/// <summary>
		/// True if the call succeeded.
		/// </summary>
		public bool Succeeded { get; private set; }

		/// <summary>
		/// The response data. Valid if Succeeded is true.
		/// </summary>
		/// <remarks>
		/// The method invoker will know what type this is, and with that type the serializer will be able to convert it to and from a byte array.
		/// </remarks>
		public byte[] Data { get; private set; }

		/// <summary>
		/// The error details. Valid if Succeeded is false.
		/// </summary>
		public string Error { get; private set; }

		public override string ToString()
		{
			var builder = new StringBuilder("SerializedResponse");
			builder.AppendLine();
			builder.Append("  CallId: ");
			builder.Append(this.CallId);
			builder.AppendLine();
			builder.Append("  Succeeded: ");
			builder.Append(this.Succeeded);
			builder.AppendLine();
			if (this.Succeeded)
			{
				builder.Append("  Data: ");
				if (this.Data == null)
				{
					builder.Append("<null>");
				}
				else
				{
					builder.Append("0x");
					builder.Append(Utilities.BytesToHexString(this.Data));
				}
			}
			else
			{
				builder.Append("  Error: ");
				builder.Append(this.Error);
			}

			return builder.ToString();
		}

		/// <summary>
		/// Creates a new success pipe response.
		/// </summary>
		/// <param name="callId">The ID of the call.</param>
		/// <param name="data">The returned data.</param>
		/// <returns>The success pipe response.</returns>
		public static SerializedPipeResponse Success(int callId, byte[] data)
		{
			return new SerializedPipeResponse { Succeeded = true, CallId = callId, Data = data };
		}

		/// <summary>
		/// Creates a new failure pipe response.
		/// </summary>
		/// <param name="callId">The ID of the call.</param>
		/// <param name="message">The failure message.</param>
		/// <returns>The failure pipe response.</returns>
		public static SerializedPipeResponse Failure(int callId, string message)
		{
			return new SerializedPipeResponse { Succeeded = false, CallId = callId, Error = message };
		}
	}
}
