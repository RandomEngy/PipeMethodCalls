using System;
using System.Collections.Generic;
using System.Text;

namespace TestAppCore
{
	public class WrappedInt
	{
		public int Num { get; set; }

		public override string ToString()
		{
			return "Wrapped " + this.Num;
		}
	}
}
