using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace GlutenFree.SignalR.ClientProxy
{
    /// <summary>
    /// Helper class
    /// Meant to help with debugging.
    /// (Also to keep IL Generation simple)
    /// </summary>
    public static class HubExtender
    {
        public static Task PerformSendCoreAsync(HubConnection c, string callName, object[] pars, CancellationToken token)
        {
            //Just return the task.
            //This is just a little weird:
            //How it works out, the IL will
            //return -this- Task to the caller from the expression
            //to be awaited.
            return c.SendCoreAsync(callName, pars, token);
        }

        

        public static Task<T> PerformInvokeCoreAsyncReturn<T>(HubConnection c,
            string callName, object[] pars, CancellationToken token)
        {
            return c.InvokeCoreAsync<T>(callName, pars, token);
        }

        public static Task PerformInvokeCoreAsyncNoReturn(HubConnection c,
            string callName, object[] pars, CancellationToken token)
        {
            return c.InvokeCoreAsync(callName, pars, token);
        }
        public static IAsyncEnumerable<T> PerformStreamCoreAsync<T>(HubConnection c,
            string callName, object[] pars, CancellationToken token)
        {
            return c.StreamAsyncCore<T>(callName, pars, token);
        }
        public static Task<IAsyncEnumerable<T>> PerformStreamCoreAsyncTask<T>(HubConnection c,
            string callName, object[] pars, CancellationToken token)
        {
            return Task.FromResult(c.StreamAsyncCore<T>(callName, pars, token));
        }

        public static Task<ChannelReader<T>> PerformChannelReadStreamCoreAsync<T>(
            HubConnection c, string callName, object[] pars,
            CancellationToken token)
        {
            return c.StreamAsChannelCoreAsync<T>(callName, pars, token);
        }
    }
    
}