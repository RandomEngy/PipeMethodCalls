using System;
using System.Collections.Generic;
using System.Text;

namespace PipeMethodCalls
{
	internal static class FuncExtensionMethods
	{
		public static void Log(this Action<string> logger, Func<string> messageFunc)
		{
			logger?.Invoke(messageFunc());
		}
	}
}
