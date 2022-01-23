using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace TestCore
{
	[DataContract]
	public class WrappedInt
	{
		[DataMember(Order = 0)]
		public int Num { get; set; }

		public override string ToString()
		{
			return "Wrapped " + this.Num;
		}
	}
}
