using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PipeMethodCalls
{
	/// <summary>
	/// Handles invoking methods over a remote pipe stream.
	/// </summary>
	/// <typeparam name="TRequesting">The request interface.</typeparam>
	internal class MethodInvoker<TRequesting> : IResponseHandler
	{
		private readonly PipeStreamWrapper pipeStream;
		private Dictionary<long, PendingCall> pendingCalls = new Dictionary<long, PendingCall>();
		private long currentCall;

		public MethodInvoker(PipeStreamWrapper pipeStream)
		{
			this.pipeStream = pipeStream;

			this.pipeStream.ResponseHandler = this;
		}

		public void HandleResponse(PipeResponse response)
		{
			if (!this.pendingCalls.TryGetValue(response.CallId, out PendingCall pendingCall))
			{
				throw new InvalidOperationException($"No pending call found for ID {response.CallId}");
			}

			pendingCall.TaskCompletionSource.TrySetResult(response);
		}

		public async Task InvokeAsync(Expression<Action<TRequesting>> expression, CancellationToken cancellationToken = default(CancellationToken))
		{
			PipeResponse response = await this.GetResponseFromExpressionAsync(expression, cancellationToken);

			if (!response.Succeeded)
			{
				// TODO: Custom exception
				throw new InvalidOperationException(response.Error);
			}
		}

		public async Task<TResult> InvokeAsync<TResult>(Expression<Func<TRequesting, TResult>> expression, CancellationToken cancellationToken = default(CancellationToken))
		{
			PipeResponse response = await this.GetResponseFromExpressionAsync(expression, cancellationToken);

			if (response.Succeeded)
			{
				if (Utilities.TryConvert(response.Data, typeof(TResult), out object result))
				{
					return (TResult)result;
				}
				else
				{
					throw new InvalidOperationException($"Unable to convert returned value to '{typeof(TResult).Name}'.");
				}
			}
			else
			{
				// TODO: Custom exception
				throw new InvalidOperationException(response.Error);
			}
		}

		public async Task InvokeAsync(Expression<Func<TRequesting, Task>> expression, CancellationToken cancellationToken = default(CancellationToken))
		{
			PipeResponse response = await this.GetResponseFromExpressionAsync(expression, cancellationToken);

			if (!response.Succeeded)
			{
				// TODO: Custom exception
				throw new InvalidOperationException(response.Error);
			}
		}

		public async Task<TResult> InvokeAsync<TResult>(Expression<Func<TRequesting, Task<TResult>>> expression, CancellationToken cancellationToken = default(CancellationToken))
		{
			PipeResponse response = await this.GetResponseFromExpressionAsync(expression, cancellationToken);

			if (response.Succeeded)
			{
				if (Utilities.TryConvert(response.Data, typeof(TResult), out object result))
				{
					return (TResult)result;
				}
				else
				{
					throw new InvalidOperationException($"Unable to convert returned value to '{typeof(TResult).Name}'.");
				}
			}
			else
			{
				// TODO: Custom exception
				throw new InvalidOperationException(response.Error);
			}
		}

		private async Task<PipeResponse> GetResponseFromExpressionAsync(Expression expression, CancellationToken cancellationToken)
		{
			PipeRequest request = this.CreateRequest(expression);
			return await this.GetResponseAsync(request, cancellationToken).ConfigureAwait(false);
		}

		private PipeRequest CreateRequest(Expression expression)
		{
			this.currentCall++;

			if (!(expression is LambdaExpression lamdaExp))
			{
				throw new ArgumentException("Only supports lambda expresions, ex: x => x.GetData(a, b)");
			}

			if (!(lamdaExp.Body is MethodCallExpression methodCallExp))
			{
				throw new ArgumentException("Only supports calling methods, ex: x => x.GetData(a, b)");
			}

			return new PipeRequest
			{
				CallId = this.currentCall,
				MethodName = methodCallExp.Method.Name,
				GenericArguments = methodCallExp.Method.GetGenericArguments(),
				Parameters = methodCallExp.Arguments.Select(argumentExpression => Expression.Lambda(argumentExpression).Compile().DynamicInvoke()).ToArray()
			};
		}

		private async Task<PipeResponse> GetResponseAsync(PipeRequest request, CancellationToken cancellationToken)
		{
			var pendingCall = new PendingCall();
			this.pendingCalls.Add(request.CallId, pendingCall);

			await this.pipeStream.SendRequestAsync(request, cancellationToken).ConfigureAwait(false);

			cancellationToken.Register(
				() =>
				{
					pendingCall.TaskCompletionSource.TrySetException(new OperationCanceledException("Request has been canceled."));
				},
				false);

			return await pendingCall.TaskCompletionSource.Task.ConfigureAwait(false);
		}
	}
}
