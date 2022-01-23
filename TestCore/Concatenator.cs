using System;
using System.Collections.Generic;
using System.Text;

namespace TestCore
{
	public class Concatenator : IConcatenator
	{
		public string Concatenate(string a, string b)
		{
			return a + b;
		}
	}
}
