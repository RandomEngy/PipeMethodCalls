using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls.MessagePack
{
	/// <summary>
	/// Serializes pipe method call information with MessagePack.
	/// </summary>
	public class MessagePackPipeSerializer : IPipeSerializer
	{
		public object Deserialize(byte[] data, Type type)
		{
			if (data.Length == 0)
			{
				return null;
			}


			return MessagePackSerializer.Deserialize(type, data);
		}

		public byte[] Serialize(object o)
		{
			if (o == null || o.GetType().FullName == "System.Threading.Tasks.VoidTaskResult")
			{
				return Array.Empty<byte>();
			}

			return MessagePackSerializer.Serialize(o.GetType(), o);
		}
	}
}
