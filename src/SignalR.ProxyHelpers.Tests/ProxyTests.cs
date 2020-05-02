using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GlutenFree.SignalR.ClientProxy;
using GlutenFree.SignalR.ClientReceiver;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using SignalR.ProxyHelpers.IntegrationTest;
using Xunit;

namespace SignalR.ProxyHelpers.Tests
{
    public static class ProxyTestHostHolder
    {
        public const int hostPort = 19002;
        public static readonly string _expressionHubString = $"http://localhost:{hostPort}/expressionTesthub";
        public static readonly string _mainHubString = $"http://localhost:{hostPort}/testhub";
        private static IHost _host;
        private static Task hostTask;

        static ProxyTestHostHolder()
        {
            var builder = TestHostbuilder.CreateHostBuilder(new String[] { },hostPort);
            _host = builder.Build();
            hostTask = Task.Factory.StartNew(()=>_host.Run());   
        }
    }
    public class ProxyTests
    {

        public ProxyTests()
        {
               
        }
        [Fact]
        public async Task SignalRHelper_Can_Communicate_Client_Server()
        {
            await _SignalRHelper_Can_Communicate_Client_Server();
        }


        private  async Task _SignalRHelper_Can_Communicate_Client_Server()
        {
            
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc);
            await client.StringType("hello");
            SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(2));
            Assert.True(GroupHub.callCounter["hello"] > 0);
        }
        [Fact]
        public async Task TwoWay_Comms_Work_Sync()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc);
            var res = client.TestTwoWaySync("nerp");
            Assert.Contains("nerp", res);
            Assert.Contains("derp", res);
        }

        [Fact]
        public async Task SendMethod_Works_Over_invoke()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc,true);
            
            await client.StringType("invoke");
            SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(2));
            Assert.True(GroupHub.callCounter["invoke"] > 0);
            
        }
        [Fact]
        public async void Void_Invokes_Work()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc,true);
            var guid = Guid.NewGuid();
            client.VoidValueType(guid);
            SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(2));
            Assert.True(GroupHub.callCounter[guid.ToString()] > 0);
            
        }
        
        [Fact]
        public async void Void_Sends_Work()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc);
            var guid = Guid.NewGuid();
            client.VoidValueType(guid);
            SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(2));
            Assert.True(GroupHub.callCounter[guid.ToString()] > 0);
            
        }
        [Fact]
        public async Task TwoWay_Comms_Work_ASync()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc);
            var res = await client.TestTwoWay("nerp");
            Assert.Contains("nerp", res);
            Assert.Contains("derp", res);
        }
        
        [Fact]
        public async Task Streaming_From_Server_works()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc);
            var res = client.StreamFromServer("nerp");
            List<int> results = new List<int>();
            await foreach (var myInt in res)
            {
                results.Add(myInt);
            }
            Assert.Equal(5,results.Count);
        }
        
        [Fact]
        public async Task Streaming_From_Server_Task_works()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc);
            var res = await client.StreamFromServerAsync("nerp");
            List<int> results = new List<int>();
            await foreach (var myInt in res)
            {
                results.Add(myInt);
            }
            Assert.Equal(5,results.Count);
        }
        
        [Fact]
        public async Task Streaming_From_Server_Channel_Task_works()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc);
            var res = await client.ReadChannelTask();
            List<int> results = new List<int>();
            await foreach (var myInt in res.ReadAllAsync())
            {
                results.Add(myInt);
            }
            Assert.Equal(5,results.Count);
        }
        
        [Fact]
        public async Task Streaming_From_Server_Channel_works()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc);
            var res = client.ReadChannel();
            List<int> results = new List<int>();
            await foreach (var myInt in res.ReadAllAsync())
            {
                results.Add(myInt);
            }
            Assert.Equal(5,results.Count);
        }
        
        [Fact]
        public async Task Streaming_From_Server_CancelToken_works()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc);
            var cts = new CancellationTokenSource();
            var res = HubProxy.WithCancellationToken(client, cts.Token,
                r => r.StreamFromServer("merp"));
            //var res = client.StreamFromServer("nerp");
            List<int> results = new List<int>();
            
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                var counter = 0;
                await foreach (var myInt in res)
                {
                    results.Add(myInt);
                    counter = counter + 1;
                    if (counter==2)
                    cts.Cancel();
                }
            });
            Assert.Equal(2,results.Count);
        }

        [Fact]
        public async Task Streaming_To_Server_works_Under_Invoke()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc, true);
            await client.StreamToServer(clientStreamData(), "nerp");
            Assert.Equal(5, GroupHub.streamCounter["nerp"].Count);
        }
        
        [Fact]
        public async Task Streaming_To_Server_works_Under_Send()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc, false);
            await client.StreamToServer(clientStreamData(), "streamserver");
            await Task.Delay(TimeSpan.FromSeconds(5));
            Assert.Equal(5, GroupHub.streamCounter["streamserver"].Count);
        }
        
        [Fact]
        public async Task AlwaysInvokeAttribute_Is_Respected()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc, false);
            await client.StreamToServerInvoke(clientStreamData(), "ainvoke");
            //await Task.Delay(TimeSpan.FromSeconds(5));
            Assert.Equal(5, GroupHub.streamCounter["ainvoke"].Count);
        }
        
        [Fact]
        public async Task AlwaysSendAttribute_Is_Respected()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc, true);
            await client.StreamToServerSend(clientStreamData(), "asend");
            
            Assert.Throws<KeyNotFoundException>(() =>
                GroupHub.streamCounter["asend"].Count);
            await Task.Delay(TimeSpan.FromSeconds(5));
            Assert.Equal(5, GroupHub.streamCounter["asend"].Count);
        }
        async IAsyncEnumerable<int> clientStreamData()
        {
            for (var i = 0; i < 5; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(0.25));
                yield return i;
            }
            //After the for loop has completed and the local function exits the stream completion will be sent.
        }
        [Fact]
        public async Task SignalRHelper_HubReceiverContainer_Gets_From_Server()
        {
            await _SignalRHelper_HubReceiverContainer_Gets_From_Server();
        }

        private  async Task _SignalRHelper_HubReceiverContainer_Gets_From_Server()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc);
            var hubRec = new HubReceiverContainer<IFoo>(hc, new FooImpl());
            await client.StringType("hello");
            SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(2));
            Assert.True(FooImpl.callCounter["hello"] > 0);
            GC.KeepAlive(hubRec);
            hubRec.Dispose();
        }

        [Fact]
        public async Task SingleValueType()
        {
            await _singleValueType();
        }

        private  async Task _singleValueType()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc);
            var hubRec = new HubReceiverContainer<IFoo>(hc, new FooImpl());
            var theGuid = Guid.NewGuid();
            await client.ValueType(theGuid);
            SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(2));
            Assert.True(GroupHub.callCounter[theGuid.ToString()] > 0);
            Assert.True(FooImpl.callCounter[theGuid.ToString()] > 0);
            GC.KeepAlive(hubRec);
            hubRec.Dispose();
        }

        [Fact]
        public async Task MultiParam()
        {
            await _multiParam();
        }

        private  async Task _multiParam()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl(ProxyTestHostHolder._mainHubString).Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc);
            var hubRec = new HubReceiverContainer<IFoo>(hc, new FooImpl());
            var theGuid = Guid.NewGuid();
            await client.MultiParam("multi", theGuid);
            SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(2));
            Assert.True(GroupHub.callCounter["multi"+theGuid] > 0);
            Assert.True(FooImpl.callCounter["multi"+theGuid] > 0);
            GC.KeepAlive(hubRec);
            hubRec.Dispose();
        }
        
    }

    public class FooImpl : IFoo
    {
        public static ConcurrentDictionary<string,int> callCounter = new ConcurrentDictionary<string, int>();

        public Task StringType(string nerp)
        {
            callCounter.AddOrUpdate(nerp, n => 1, (n, i) => i + 1);
            return Task.CompletedTask;
        }

        public Task ValueType(Guid val)
        {
            callCounter.AddOrUpdate(val.ToString(), n => 1, (n, i) => i + 1);
            return Task.CompletedTask;
        }

        public Task MultiParam(string nerp, Guid guid)
        {
            callCounter.AddOrUpdate(nerp+guid.ToString(), n => 1, (n, i) => i + 1);
            return Task.CompletedTask;
        }
    }
}