using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace GlutenFree.SignalR.ClientProxy
{
    public static class HubProxy
    {
        /// <summary>
        /// Creates a Proxy
        /// </summary>
        /// <param name="conn">The <see cref="HubConnection"/> to use for the proxy</param>
        /// <param name="alwaysInvoke">if -true- <see cref="Void"/> and <see cref="Task"/> returning calls will be treated as hub invocations rather than sends.</param>
        /// <typeparam name="TProxy">The type of the proxy to create</typeparam>
        /// <returns></returns>
        public static TProxy Create<TProxy>(HubConnection conn,
            bool alwaysInvoke = false)
        {
            return HubProxyBuilder.CreateProxy<TProxy>(conn, alwaysInvoke);
        }
        internal static AsyncLocal<CancellationToken?> _localCancel = new AsyncLocal<CancellationToken?>();
        
        public static CancellationToken ContextCancellationToken()
        {
            return _localCancel.Value ?? CancellationToken.None;
        }

        /// <summary>
        /// Calls a HubMethod using a given cancellation Token
        /// </summary>
        /// <param name="proxy">The Proxy to use</param>
        /// <param name="token">The token to call with</param>
        /// <param name="action">The Call to the proxy</param>
        /// <typeparam name="TProxy">The Proxy</typeparam>
        /// <typeparam name="TResult">The Type of result</typeparam>
        /// <returns></returns>
        public static TResult WithCancellationToken<TProxy, TResult>(TProxy proxy, CancellationToken token, Func<TProxy,TResult> action)
        {
            try
            {
                _localCancel.Value = token;
                return action(proxy);
            }
            finally
            {
                _localCancel.Value = null;
            }
        }
        //I don't think we really need these... do we?
        /*public static Task WithCancellationToken<TProxy>(TProxy proxy, CancellationToken token, Func<TProxy,Task> action)
        {
            try
            {
                _localCancel.Value = token;
                return action(proxy);
            }
            finally
            {
                _localCancel.Value = null;
            }
        }
        
        public static Task<TResult> WithCancellationToken<TProxy, TResult>(TProxy proxy, CancellationToken token, Func<TProxy,Task<TResult>> action)
        {
            try
            {
                _localCancel.Value = token;
                return action(proxy);
            }
            finally
            {
                _localCancel.Value = null;
            }
        }*/
        public static void WithCancellationToken<TProxy>(TProxy proxy, CancellationToken token, Action<TProxy> action)
        {
            try
            {
                _localCancel.Value = token;
                action(proxy);
            }
            finally
            {
                _localCancel.Value = null;
            }
        }
    }
}