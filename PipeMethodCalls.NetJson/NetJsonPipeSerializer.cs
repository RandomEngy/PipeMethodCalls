using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace PipeMethodCalls.NetJson
{
	/// <summary>
	/// Serializes pipe method call information with System.Text.Json.
	/// </summary>
	public class NetJsonPipeSerializer : IPipeSerializer
	{
		public object Deserialize(byte[] data, Type type)
		{
			return JsonSerializer.Deserialize(data, type);
		}

		public byte[] Serialize(object o)
		{
			using (var memoryStream = new MemoryStream())
			using (var utf8JsonWriter = new Utf8JsonWriter(memoryStream))
			{
				JsonSerializer.Serialize(utf8JsonWriter, o);
				return memoryStream.ToArray();
			}
		}
	}
}
