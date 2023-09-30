using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCore
{
	public class Adder : IAdder
	{
		public int AddNumbers(int a, int b)
		{
			return a + b;
		}

		public double Sum(double[] numbers)
		{
			return numbers.Sum();
		}

		public WrappedInt AddWrappedNumbers(WrappedInt a, WrappedInt b)
		{
			return new WrappedInt { Num = a.Num + b.Num };
		}

		public Task<int> AddAsync(int a, int b)
		{
			return Task.FromResult(a + b);
		}

		public IList<T> Listify<T>(T item)
		{
			return new List<T> { item };
		}

		public int Unwrap(WrappedInt a)
		{
			if (a == null)
			{
				return 0;
			}

			return a.Num;
		}

		public void DoesNothing()
		{
		}

		public Task DoesNothingAsync()
		{
			return Task.CompletedTask;
		}

		public void AlwaysFails()
		{
			throw new InvalidOperationException("This method always fails.");
		}

		public void HasRefParam(ref int refParam)
		{
			refParam = 5;
		}
	}
}
