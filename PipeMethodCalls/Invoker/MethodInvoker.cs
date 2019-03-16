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
	internal class MethodInvoker<TRequesting> : IResponseHandler, IPipeInvoker<TRequesting>
		where TRequesting : class
	{
		private readonly PipeStreamWrapper pipeStreamWrapper;
		private readonly PipeHost pipeHost;
		private Dictionary<long, PendingCall> pendingCalls = new Dictionary<long, PendingCall>();
		private long currentCall;

		/// <summary>
		/// Initializes a new instance of the <see cref="MethodInvoker" /> class.
		/// </summary>
		/// <param name="pipeStreamWrapper">The pipe stream wrapper to use for invocation and response handling.</param>
		public MethodInvoker(PipeStreamWrapper pipeStreamWrapper, PipeHost pipeHost)
		{
			this.pipeStreamWrapper = pipeStreamWrapper;
			this.pipeStreamWrapper.ResponseHandler = this;

			this.pipeHost = pipeHost;
		}

		/// <summary>
		/// Handles a response message received from a remote endpoint.
		/// </summary>
		/// <param name="response">The response message to handle.</param>
		public void HandleResponse(PipeResponse response)
		{
			if (!this.pendingCalls.TryGetValue(response.CallId, out PendingCall pendingCall))
			{
				throw new InvalidOperationException($"No pending call found for ID {response.CallId}");
			}

			pendingCall.TaskCompletionSource.TrySetResult(response);
		}

		/// <summary>
		/// Invokes a method on the server.
		/// </summary>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		public async Task InvokeAsync(Expression<Action<TRequesting>> expression, CancellationToken cancellationToken = default)
		{
			// Sync, no result

			Utilities.EnsureReadyForInvoke(this.pipeHost.State, this.pipeHost.PipeFault);

			PipeResponse response = await this.GetResponseFromExpressionAsync(expression, cancellationToken);

			if (!response.Succeeded)
			{
				throw new PipeInvokeFailedException(response.Error);
			}
		}

		/// <summary>
		/// Invokes a method on the server.
		/// </summary>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		public async Task InvokeAsync(Expression<Func<TRequesting, Task>> expression, CancellationToken cancellationToken = default)
		{
			// Async, no result

			Utilities.EnsureReadyForInvoke(this.pipeHost.State, this.pipeHost.PipeFault);

			PipeResponse response = await this.GetResponseFromExpressionAsync(expression, cancellationToken);

			if (!response.Succeeded)
			{
				throw new PipeInvokeFailedException(response.Error);
			}
		}

		/// <summary>
		/// Invokes a method on the server.
		/// </summary>
		/// <typeparam name="TResult">The type of result from the method.</typeparam>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <returns>The method result.</returns>
		public async Task<TResult> InvokeAsync<TResult>(Expression<Func<TRequesting, TResult>> expression, CancellationToken cancellationToken = default)
		{
			// Sync with result

			Utilities.EnsureReadyForInvoke(this.pipeHost.State, this.pipeHost.PipeFault);

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
				throw new PipeInvokeFailedException(response.Error);
			}
		}

		/// <summary>
		/// Invokes a method on the server.
		/// </summary>
		/// <typeparam name="TResult">The type of result from the method.</typeparam>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <returns>The method result.</returns>
		public async Task<TResult> InvokeAsync<TResult>(Expression<Func<TRequesting, Task<TResult>>> expression, CancellationToken cancellationToken = default(CancellationToken))
		{
			// Async with result

			Utilities.EnsureReadyForInvoke(this.pipeHost.State, this.pipeHost.PipeFault);

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
				throw new PipeInvokeFailedException(response.Error);
			}
		}

		/// <summary>
		/// Gets a response from the given expression.
		/// </summary>
		/// <param name="expression">The expression to execute.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <returns>A response for the given expression.</returns>
		private async Task<PipeResponse> GetResponseFromExpressionAsync(Expression expression, CancellationToken cancellationToken)
		{
			PipeRequest request = this.CreateRequest(expression);
			return await this.GetResponseAsync(request, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Creates a pipe request from the given expression.
		/// </summary>
		/// <param name="expression">The expression to execute.</param>
		/// <returns>The request to send over the pipe to execute that expression.</returns>
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

		/// <summary>
		/// Gets a pipe response for the given pipe request.
		/// </summary>
		/// <param name="request">The request to send.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <returns>The pipe response.</returns>
		private async Task<PipeResponse> GetResponseAsync(PipeRequest request, CancellationToken cancellationToken)
		{
			var pendingCall = new PendingCall();
			this.pendingCalls.Add(request.CallId, pendingCall);

			await this.pipeStreamWrapper.SendRequestAsync(request, cancellationToken).ConfigureAwait(false);

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
