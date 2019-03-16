using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PipeMethodCalls
{
	/// <summary>
	/// Utility functions.
	/// </summary>
	internal static class Utilities
	{
		/// <summary>
		/// Tries to convert the given value to the given type.
		/// </summary>
		/// <param name="valueToConvert">The value to convert.</param>
		/// <param name="targetType">The target type.</param>
		/// <param name="targetValue">The variable to store the converted value in.</param>
		/// <returns>True if the conversion succeeded.</returns>
		public static bool TryConvert(object valueToConvert, Type targetType, out object targetValue)
		{
			if (targetType.IsInstanceOfType(valueToConvert))
			{
				// copy value directly if it can be assigned to targetType
				targetValue = valueToConvert;
				return true;
			}

			if (targetType.IsEnum)
			{
				if (valueToConvert is string str)
				{
					try
					{
						targetValue = Enum.Parse(targetType, str, ignoreCase: true);
						return true;
					}
					catch
					{ }
				}
				else
				{
					try
					{
						targetValue = Enum.ToObject(targetType, valueToConvert);
						return true;
					}
					catch
					{ }
				}
			}

			if (valueToConvert is string string2 && targetType == typeof(Guid))
			{
				if (Guid.TryParse(string2, out Guid result))
				{
					targetValue = result;
					return true;
				}
			}

			if (valueToConvert is JObject jObj)
			{
				// Rely on JSON.Net to convert complex type
				targetValue = jObj.ToObject(targetType);
				return true;
			}

			if (valueToConvert is JArray jArray)
			{
				targetValue = jArray.ToObject(targetType);
				return true;
			}

			try
			{
				targetValue = Convert.ChangeType(valueToConvert, targetType);
				return true;
			}
			catch
			{ }

			try
			{
				targetValue = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(valueToConvert), targetType);
				return true;
			}
			catch
			{ }

			targetValue = null;
			return false;
		}

		/// <summary>
		/// Ensures the pipe state is ready to invoke methods.
		/// </summary>
		/// <param name="state"></param>
		/// <param name="pipeFault"></param>
		public static void EnsureReadyForInvoke(PipeState state, Exception pipeFault)
		{
			if (state == PipeState.NotOpened)
			{
				throw new PipeInvokeFailedException("Can only invoke methods after connecting the pipe.");
			}
			else if (state == PipeState.Closed)
			{
				throw new PipeInvokeFailedException("Cannot invoke methods after the pipe has closed.");
			}
			else if (state == PipeState.Faulted)
			{
				throw new PipeInvokeFailedException("Cannot invoke method. Pipe has faulted.", pipeFault);
			}
		}
	}
}
