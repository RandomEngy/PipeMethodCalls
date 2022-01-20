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
			return MessagePackSerializer.Deserialize(type, data);
		}

		public byte[] Serialize(object o)
		{
			return MessagePackSerializer.Serialize(o.GetType(), o);
		}
	}
}
