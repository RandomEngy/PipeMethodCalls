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

		int Unwrap(WrappedInt a);

		void DoesNothing();

		Task DoesNothingAsync();

		void AlwaysFails();

		void HasRefParam(ref int refParam);
	}
}
