using System.Threading;
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
            //But how it works out, the IL
            return c.SendCoreAsync(callName, pars, token);
        }
    }
}