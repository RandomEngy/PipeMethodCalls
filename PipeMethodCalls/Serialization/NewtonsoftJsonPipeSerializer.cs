using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	internal class NewtonsoftJsonPipeSerializer : IPipeSerializer
	{
		private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore
		};

		public object Deserialize(byte[] data, Type type)
		{
			return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(data), type, serializerSettings);
		}

		public byte[] Serialize(object o)
		{
			string json = JsonConvert.SerializeObject(o, serializerSettings);
			return Encoding.UTF8.GetBytes(json);
		}
	}
}
