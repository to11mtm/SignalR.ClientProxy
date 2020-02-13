using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace SignalR.ClientProxy
{
    public static class HubExtender
    {
        public static Task PerformSendCoreAsync(HubConnection c, string callName, object[] pars, CancellationToken token)
        {
            return c.SendCoreAsync(callName, pars, token);
        }
    }
}