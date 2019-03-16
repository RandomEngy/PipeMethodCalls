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
				// TODO: handle error
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
		/// Ensures the invoker is non-null.
		/// </summary>
		/// <typeparam name="T">The type of invoker.</typeparam>
		/// <param name="invoker">The invoker to check.</param>
		public static void CheckInvoker<T>(MethodInvoker<T> invoker)
		{
			if (invoker == null)
			{
				throw new InvalidOperationException("Can only invoke operations after calling ConnectAsync and before calling Dispose.");
			}
		}
	}
}
