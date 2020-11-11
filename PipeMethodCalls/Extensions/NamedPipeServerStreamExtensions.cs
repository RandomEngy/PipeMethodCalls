using System;
using System.IO.Pipes;

namespace PipeMethodCalls
{
    /// <summary>
    /// Extensions to make upgrading and working with NamedPipeServerStream easier.
    /// </summary>
    public static class NamedPipeServerStreamExtensions
    {
        /// <summary>
        /// Method of quickly upgrading an existing <see cref="NamedPipeServerStream"/> into a <see cref="PipeServerWithCallback{TRequesting,THandling}"/>.
        /// </summary>
        /// <param name="existingPipe">Instance of a named pipe to upgrade.</param>
        /// <param name="handlerFactoryFunc">A factory function to provide the handler implementation.</param>
        /// <typeparam name="TRequesting">The callback channel interface that the client will be handling.</typeparam>
        /// <typeparam name="THandling">The interface for requests that this server will be handling.</typeparam>
        /// <returns></returns>
        public static PipeServerWithCallback<TRequesting, THandling> UpgradeWithCallback<TRequesting, THandling>(
            this NamedPipeServerStream existingPipe, Func<THandling> handlerFactoryFunc)
            where TRequesting : class where THandling : class
        {
            return new PipeServerWithCallback<TRequesting, THandling>(existingPipe, handlerFactoryFunc);
        }

        /// <summary>
        /// Method of quickly upgrading an existing <see cref="NamedPipeServerStream"/> into a <see cref="PipeServer{THandling}"/>.
        /// </summary>
        /// <param name="existingPipe">Instance of a named pipe to upgrade.</param>
        /// <param name="handlerFactoryFunc">A factory function to provide the handler implementation.</param>
        /// <typeparam name="THandling">The interface for requests that this server will be handling.</typeparam>
        /// <returns></returns>
        public static PipeServer<THandling> Upgrade<THandling>(
            this NamedPipeServerStream existingPipe, Func<THandling> handlerFactoryFunc)
            where THandling : class
        {
            return new PipeServer<THandling>(existingPipe, handlerFactoryFunc);
        }
    }
}