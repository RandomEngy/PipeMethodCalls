using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	/// <summary>
	/// Defines how to serialize and deserialize objects to pass through the pipe.
	/// </summary>
	public interface IPipeSerializer
	{
		byte[] Serialize(object o);

		object Deserialize(byte[] data, Type type);
	}
}
