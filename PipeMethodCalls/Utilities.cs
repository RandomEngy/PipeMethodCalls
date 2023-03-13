using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	/// <summary>
	/// Utility functions.
	/// </summary>
	internal static class Utilities
	{
		/// <summary>
		/// Ensures the pipe state is ready to invoke methods.
		/// </summary>
		/// <param name="state"></param>
		/// <param name="pipeFault"></param>
		public static void EnsureReadyForInvoke(PipeState state, Exception pipeFault)
		{
			if (state == PipeState.NotOpened)
			{
				throw new IOException("Can only invoke methods after connecting the pipe.");
			}
			else if (state == PipeState.Closed)
			{
				throw new IOException("Cannot invoke methods after the pipe has closed.");
			}
			else if (state == PipeState.Faulted)
			{
				throw new IOException("Cannot invoke method. Pipe has faulted.", pipeFault);
			}
		}

		/// <summary>
		/// Ensures the provided raw server pipe is compatible with method call functionality.
		/// </summary>
		/// <param name="rawPipe">Raw pipe stream to test for method call capability.</param>
		/// <exception cref="ArgumentException">Throws if <see cref="NamedPipeServerStream"/> is not compatible.</exception>
		/// <remarks>The pipe also needs to be set up with PipeOptions.Asynchronous but we cannot check for that directly since IsAsync returns the wrong value.</remarks>
		public static void ValidateRawServerPipe(NamedPipeServerStream rawPipe)
		{
			ValidateRawPipe(rawPipe);
			if (rawPipe.TransmissionMode != PipeTransmissionMode.Byte)
			{
				throw new ArgumentException("Provided pipe cannot be wrapped. Pipe needs to be setup with PipeTransmissionMode.Byte", nameof(rawPipe));
			}
		}

		/// <summary>
		/// Ensures the provided raw client pipe is compatible with method call functionality.
		/// </summary>
		/// <param name="rawPipe">Raw pipe stream to test for method call capability.</param>
		/// <exception cref="ArgumentException">Throws if <see cref="NamedPipeServerStream"/> is not compatible.</exception>
		/// <remarks>The pipe also needs to be set up with PipeOptions.Asynchronous but we cannot check for that directly since IsAsync returns the wrong value.</remarks>
		public static void ValidateRawClientPipe(NamedPipeClientStream rawPipe)
		{
			ValidateRawPipe(rawPipe);
		}

		/// <summary>
		/// Ensures the provided raw pipe is compatible with method call functionality.
		/// </summary>
		/// <param name="rawPipe">Raw pipe stream to test for method call capability.</param>
		/// <exception cref="ArgumentException">Throws if <see cref="PipeStream"/> is not compatible.</exception>
		private static void ValidateRawPipe(PipeStream rawPipe)
		{
			if (!rawPipe.CanRead || !rawPipe.CanWrite)
			{
				throw new ArgumentException("Provided pipe cannot be wrapped. Pipe needs to be setup with PipeDirection.InOut", nameof(rawPipe));
			}
		}

		/// <summary>
		/// Convert a byte array to a hex string.
		/// </summary>
		/// <param name="bytes">The byte array to convert.</param>
		/// <returns>The hex string.</returns>
		internal static string BytesToHexString(byte[] bytes)
		{
			char[] c = new char[bytes.Length * 2];
			int b;
			for (int i = 0; i < bytes.Length; i++)
			{
				b = bytes[i] >> 4;
				c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
				b = bytes[i] & 0xF;
				c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
			}
			return new string(c);
		}
	}
}
