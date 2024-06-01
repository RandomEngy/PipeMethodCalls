using System;
using System.Collections.Generic;
using System.Text;

namespace TestCore
{
	public class Crasher : ICrasher
	{
		public void Crash()
		{
			Environment.Exit(1);
		}
	}
}
