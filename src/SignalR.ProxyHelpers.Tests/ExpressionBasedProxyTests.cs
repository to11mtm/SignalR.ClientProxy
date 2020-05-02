using System;
using System.Threading;
using System.Threading.Tasks;
using GlutenFree.SignalR.ClientProxy;
using GlutenFree.SignalR.ClientProxy.ExpressionBased;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using SignalR.ProxyHelpers.IntegrationTest;
using Xunit;

namespace SignalR.ProxyHelpers.Tests
{
    public class ExpressionBasedProxyTests
    {

        public ExpressionBasedProxyTests()
        {
            
            
        }
        [Fact]
        public async Task ExpressionBasedHubProxy_Proxies()
        {
            await _ExpressionBasedHubProxy_Can_Communicate_Client_Server();
        }

        private async Task
            _ExpressionBasedHubProxy_Can_Communicate_Client_Server()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._expressionHubString).Build();
            await hc.StartAsync();
            var client = new ExpressionBasedHubProxy<IBar>(hc);

            await client.Execute(r => r.StringType("hello2"));
            SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(2));
            Assert.True(GroupHub.callCounter["hello2"] > 0);
        }

        [Fact]
        public void ExpressionBasedHubProxy_Throws_If_Not_Given_Proxy_Call()
        {
            var client = new ExpressionBasedHubProxy<IBar>(null);
            Assert.Throws<ArgumentException>(() =>
                client.Execute(r => Console.Write("")));
        }

        [Fact]
        public async Task
            ExpressionBasedHubProxy_Can_TwoWay_Client_Server_ASync()
        {
            await _ExpressionBasedHubProxy_Can_TwoWay_Client_Server();
        }

        private async Task _ExpressionBasedHubProxy_Can_TwoWay_Client_Server()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._expressionHubString).Build();
            await hc.StartAsync();
            var client = new ExpressionBasedHubProxy<IBar>(hc);

            var res = await client.Invoke(r => r.TestTwoWay("derp"));
            Assert.Contains("derp", res);
        }

        [Fact]
        public void
            ExpressionBasedHubProxy_Can_TwoWay_Client_Server_Sync()
        {
            _ExpressionBasedHubProxy_Can_TwoWay_Client_Server_Sync();
        }

        private void _ExpressionBasedHubProxy_Can_TwoWay_Client_Server_Sync()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._expressionHubString).Build();
            hc.StartAsync().GetAwaiter().GetResult();
            var client = new ExpressionBasedHubProxy<IBar>(hc);

            var res = client.Invoke(r => r.TestTwoWaySync("derp"));
            Assert.Contains("derp", res);
        }

        [Fact]
        public async Task
            ExpressionBasedHubProxy_Can_TwoWay_Client_Server_Sync_Over_Async()
        {
            await
                _ExpressionBasedHubProxy_Can_TwoWay_Client_Server_Sync_Over_Async();
        }

        private async Task
            _ExpressionBasedHubProxy_Can_TwoWay_Client_Server_Sync_Over_Async()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._expressionHubString).Build();
            hc.StartAsync().GetAwaiter().GetResult();
            var client = new ExpressionBasedHubProxy<IBar>(hc);

            var res = await client.InvokeAsync(r => r.TestTwoWaySync("derp"));
            Assert.Contains("derp", res);
        }

        [Fact]
        public async Task
            ExpressionBasedHubProxy_Can_TwoWay_Client_Server_ASync_Over_Async()
        {
            await
                _ExpressionBasedHubProxy_Can_TwoWay_Client_Server_ASync_Over_Async();
        }

        private async Task
            _ExpressionBasedHubProxy_Can_TwoWay_Client_Server_ASync_Over_Async()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._expressionHubString).Build();
            hc.StartAsync().GetAwaiter().GetResult();
            var client = new ExpressionBasedHubProxy<IBar>(hc);

            var res = await client.InvokeAsync(r => r.TestTwoWay("derp"));
            Assert.Contains("derp", res);
        }
    }
}