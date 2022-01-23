using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TestCore
{
	public interface IAdder
	{
		int AddNumbers(int a, int b);

		WrappedInt AddWrappedNumbers(WrappedInt a, WrappedInt b);

		Task<int> AddAsync(int a, int b);

		IList<T> Listify<T>(T item);

		void AlwaysFails();

		void HasRefParam(ref int refParam);
	}
}
