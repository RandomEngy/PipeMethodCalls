using System;
using System.Collections.Generic;
using System.Text;

namespace TestAppCore
{
	public class Adder : IAdder
	{
		public int AddNumbers(int a, int b)
		{
			return a + b;
		}

		public WrappedInt AddWrappedNumbers(WrappedInt a, WrappedInt b)
		{
			return new WrappedInt {Num = a.Num + b.Num};
		}
	}
}
