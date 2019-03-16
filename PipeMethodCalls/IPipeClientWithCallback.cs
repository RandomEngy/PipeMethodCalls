using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	public interface IPipeClientWithCallback<TRequesting>
	{
		Task ConnectAsync(CancellationToken cancellationToken = default);
		Task InvokeAsync(Expression<Action<TRequesting>> expression, CancellationToken cancellationToken = default);
		Task InvokeAsync(Expression<Func<TRequesting, Task>> expression, CancellationToken cancellationToken = default);
		Task<TResult> InvokeAsync<TResult>(Expression<Func<TRequesting, Task<TResult>>> expression, CancellationToken cancellationToken = default);
		Task<TResult> InvokeAsync<TResult>(Expression<Func<TRequesting, TResult>> expression, CancellationToken cancellationToken = default);
		void SetLogger(Action<string> logger);
		Task WaitForRemotePipeCloseAsync(CancellationToken cancellationToken = default);
	}
}