using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	internal static class StreamExtensions
	{
		/// <summary>
		/// Writes a varint to the stream.
		/// </summary>
		/// <remarks>See https://protobuf.dev/programming-guides/encoding/#varints</remarks>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="val">The number to write.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the value is negative. varints are supposed to be unsigned.</exception>
		public static void WriteVarInt(this Stream stream, int val)
		{
			byte[] bytes = Utilities.GetVarInt(val);
			stream.Write(bytes, 0, bytes.Length);
		}

		/// <summary>
		/// Synchronously reads a varint from a stream.
		/// </summary>
		/// <remarks>See https://protobuf.dev/programming-guides/encoding/#varints</remarks>
		/// <param name="stream">The stream to read from.</param>
		/// <returns>The read number.</returns>
		public static int ReadVarInt(this Stream stream)
		{
			int size = 0;
			int sizeByteShift = 0;
			while (true)
			{
				int sizeByte = stream.ReadByte();

				// Strip the continuation bit
				int sizeBytePayload = sizeByte & 0x7f;

				// Least significant bytes are given first, so we shift left
				// further as we continue to read size bytes.
				int sizeByteAmount = sizeBytePayload << sizeByteShift;

				// Add the 7 payload bytes to the size
				size += sizeByteAmount;

				if ((sizeByte & 0x80) > 0)
				{
					// The continuation bit is set. Keep on reading bytes to determine the size.
					sizeByteShift += 7;
				}
				else
				{
					// No continuation bit. We're done determining the size.
					break;
				}
			}

			return size;
		}

		/// <summary>
		/// Asynchronously reads a var int from a stream.
		/// </summary>
		/// <remarks>See https://protobuf.dev/programming-guides/encoding/#varints</remarks>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>The read number.</returns>
		public static async Task<int> ReadVarIntAsync(this Stream stream, CancellationToken cancellationToken)
		{
			byte[] lengthReadBuffer = new byte[1];

			int size = 0;
			int sizeByteShift = 0;
			while (true)
			{
				await stream.ReadAsync(lengthReadBuffer, 0, 1, cancellationToken).ConfigureAwait(false);
				int sizeByte = lengthReadBuffer[0];

				// Strip the continuation bit
				int sizeBytePayload = sizeByte & 0x7f;

				// Least significant bytes are given first, so we shift left
				// further as we continue to read size bytes.
				int sizeByteAmount = sizeBytePayload << sizeByteShift;

				// Add the 7 payload bytes to the size
				size += sizeByteAmount;

				if ((sizeByte & 0x80) > 0)
				{
					// The continuation bit is set. Keep on reading bytes to determine the size.
					sizeByteShift += 7;
				}
				else
				{
					// No continuation bit. We're done determining the size.
					break;
				}
			}

			return size;
		}

		public static void WriteArray(this Stream stream, byte[][] arr)
		{
			stream.WriteVarInt(arr.Length);
			foreach (byte[] itemBytes in arr)
			{
				stream.WriteVarInt(itemBytes.Length);
				stream.Write(itemBytes, 0, itemBytes.Length);
			}
		}

		public static byte[][] ReadArray(this Stream stream)
		{
			int arrayLength = stream.ReadVarInt();
			byte[][] result = new byte[arrayLength][];
			for (int i = 0; i < arrayLength; i++)
			{
				int payloadLength = stream.ReadVarInt();
				byte[] payloadBytes = new byte[payloadLength];
				stream.Read(payloadBytes, 0, payloadLength);
				result[i] = payloadBytes;
			}

			return result;
		}

		public static void WriteUtf8String(this Stream stream, string str)
		{
			byte[] strBytes = Encoding.UTF8.GetBytes(str);
			stream.Write(strBytes, 0, strBytes.Length);

			// Null-terminate
			stream.WriteByte(0);
		}

		public static string ReadUtf8String(this Stream stream)
		{
			long originalPosition = stream.Position;
			while (stream.ReadByte() != 0)
			{
			}

			long positionAfterReadingZero = stream.Position;

			// Go back to where we started and read as a string
			stream.Seek(originalPosition, SeekOrigin.Begin);

			byte[] utf8Bytes = new byte[positionAfterReadingZero - originalPosition - 1];

			stream.Read(utf8Bytes, 0, utf8Bytes.Length);

			// Read the null to get us past it
			stream.ReadByte();

			return Encoding.UTF8.GetString(utf8Bytes);
		}
	}
}
