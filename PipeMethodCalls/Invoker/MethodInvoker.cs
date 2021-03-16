﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
        /// <summary>
        /// Hack : Exception are not properly serialize/deserialize in JSON, we need to recreate them on the pipeClient, therefore we need Coveo.CES.CustomCrawlers assembly.
        /// </summary>
        private const string COVEOCUSTOMCRAWLERS_ASSEMBLY = "Coveo.CES.CustomCrawlers, Version=8.0.0.0, Culture=neutral, PublicKeyToken=null";

        private const string COVEO_EXCEPTION = "Coveo";
        private const string ARGUMENT_EXCEPTION = "Argument";
		private readonly PipeStreamWrapper pipeStreamWrapper;
		private readonly PipeMessageProcessor pipeHost;
		private Dictionary<long, PendingCall> pendingCalls = new Dictionary<long, PendingCall>();

		// Lock object for accessing pending calls dictionary.
		private object pendingCallsLock = new object();

		private long currentCall;

		/// <summary>
		/// Initializes a new instance of the <see cref="MethodInvoker" /> class.
		/// </summary>
		/// <param name="pipeStreamWrapper">The pipe stream wrapper to use for invocation and response handling.</param>
		public MethodInvoker(PipeStreamWrapper pipeStreamWrapper, PipeMessageProcessor pipeHost)
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
			PendingCall pendingCall = null;

			lock (this.pendingCallsLock)
			{
				if (this.pendingCalls.TryGetValue(response.CallId, out pendingCall))
				{
					// Call has completed. Remove from pending list.
					this.pendingCalls.Remove(response.CallId);
				}
				else
				{
					throw new InvalidOperationException($"No pending call found for ID {response.CallId}");
				}
			}

			// Mark method call task as completed.
			pendingCall.TaskCompletionSource.TrySetResult(response);
		}

		/// <summary>
		/// Invokes a method on the server.
		/// </summary>
		/// <param name="expression">The method to invoke.</param>
		/// <param name="cancellationToken">A token to cancel the request.</param>
		/// <exception cref="PipeInvokeFailedException">Thrown when the invoked method throws an exception.</exception>
		/// <exception cref="IOException">Thrown when there is an issue with the pipe communication.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the cancellation token is invoked.</exception>
		public async Task InvokeAsync(Expression<Action<TRequesting>> expression, CancellationToken cancellationToken = default)
		{
			// Sync, no result

			Utilities.EnsureReadyForInvoke(this.pipeHost.State, this.pipeHost.PipeFault);

			PipeResponse response = await this.GetResponseFromExpressionAsync(expression, cancellationToken).ConfigureAwait(false);

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
		/// <exception cref="PipeInvokeFailedException">Thrown when the invoked method throws an exception.</exception>
		/// <exception cref="IOException">Thrown when there is an issue with the pipe communication.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the cancellation token is invoked.</exception>
		public async Task InvokeAsync(Expression<Func<TRequesting, Task>> expression, CancellationToken cancellationToken = default)
		{
			// Async, no result

			Utilities.EnsureReadyForInvoke(this.pipeHost.State, this.pipeHost.PipeFault);

			PipeResponse response = await this.GetResponseFromExpressionAsync(expression, cancellationToken).ConfigureAwait(false);

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
		/// <exception cref="PipeInvokeFailedException">Thrown when the invoked method throws an exception.</exception>
		/// <exception cref="IOException">Thrown when there is an issue with the pipe communication.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the cancellation token is invoked.</exception>
		public async Task<TResult> InvokeAsync<TResult>(Expression<Func<TRequesting, TResult>> expression, CancellationToken cancellationToken = default)
		{
			// Sync with result

			Utilities.EnsureReadyForInvoke(this.pipeHost.State, this.pipeHost.PipeFault);

			PipeResponse response = await this.GetResponseFromExpressionAsync(expression, cancellationToken).ConfigureAwait(false);

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
                ThrowCoveoException(response);
                ThrowGenericException(response);

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
		/// <exception cref="PipeInvokeFailedException">Thrown when the invoked method throws an exception.</exception>
		/// <exception cref="IOException">Thrown when there is an issue with the pipe communication.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the cancellation token is invoked.</exception>
		public async Task<TResult> InvokeAsync<TResult>(Expression<Func<TRequesting, Task<TResult>>> expression, CancellationToken cancellationToken = default)
		{
			// Async with result

			Utilities.EnsureReadyForInvoke(this.pipeHost.State, this.pipeHost.PipeFault);

			PipeResponse response = await this.GetResponseFromExpressionAsync(expression, cancellationToken).ConfigureAwait(false);

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

		private void ThrowCoveoException(PipeResponse response)
		{
			if (response.Exception != null
				&& !string.IsNullOrEmpty(response.Exception.ExceptionType)
				&& response.Exception.ExceptionType.Contains(COVEO_EXCEPTION))
			{
				var m_Assembly = Assembly.Load(COVEOCUSTOMCRAWLERS_ASSEMBLY);
				var type = m_Assembly.GetType(response.Exception.ExceptionType);

				if (response.Exception.InnerException != null)
				{
					var innerType = m_Assembly.GetType(response.Exception.InnerException.ExceptionType);
					var innerException = Activator.CreateInstance(innerType, response.Exception.InnerException.ExceptionMessage) as Exception;
					throw Activator.CreateInstance(type, response.Exception.ExceptionMessage, innerException) as Exception;
				}

				throw Activator.CreateInstance(type, response.Exception.ExceptionMessage) as Exception;
			}
		}

		private void ThrowGenericException(PipeResponse response)
		{
			if (response.Exception != null && !string.IsNullOrEmpty(response.Exception.ExceptionType))
			{
				var type = Type.GetType(response.Exception.ExceptionType);

				if (response.Exception.InnerException != null)
				{
					var innerType = Type.GetType(response.Exception.InnerException.ExceptionType);
					var innerException = Activator.CreateInstance(innerType, response.Exception.InnerException.ExceptionMessage) as Exception;
					throw Activator.CreateInstance(type, response.Exception.ExceptionMessage, innerException) as Exception;
				}

				if (response.Exception.ExceptionType.Contains(ARGUMENT_EXCEPTION))
					throw Activator.CreateInstance(type, null, response.Exception.ExceptionMessage) as Exception;

				throw Activator.CreateInstance(type, response.Exception.ExceptionMessage) as Exception;
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
			long callId = Interlocked.Increment(ref this.currentCall);

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
				CallId = callId,
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

			lock (this.pendingCallsLock)
			{
				this.pendingCalls.Add(request.CallId, pendingCall);
			}

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
