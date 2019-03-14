using System;
using System.Collections.Generic;
using System.Text;

namespace TestAppCore
{
	public interface IAdder
	{
		int AddNumbers(int a, int b);

		WrappedInt AddWrappedNumbers(WrappedInt a, WrappedInt b);
	}
}
