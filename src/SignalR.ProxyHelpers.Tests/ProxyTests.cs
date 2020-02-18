using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using GlutenFree.SignalR.ClientProxy;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using SignalR.ProxyHelpers.IntegrationTest;
using Xunit;

namespace SignalR.ProxyHelpers.Tests
{
    public class ProxyTests : IDisposable
    {
        private static IHost _host;

        public ProxyTests()
        {
            
            var builder = TestHostbuilder.CreateHostBuilder(new String[] { });
            _host = builder.Build();
            Task.Factory.StartNew(()=>_host.Run());   
        }
        [Fact]
        public async Task SignalRHelper_Can_Communicate_Client_Server()
        {
            await _SignalRHelper_Can_Communicate_Client_Server();
        }

        private  async Task _SignalRHelper_Can_Communicate_Client_Server()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl("http://localhost:19001/testhub").Build();
            await hc.StartAsync();
            var client = HubProxyBuilder.CreateProxy<IBar>(hc);
            await client.StringType("hello");
            SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(2));
            Assert.True(GroupHub.callCounter["hello"] > 0);
        }

        [Fact]
        public async Task SignalRHelper_HubReceiverContainer_Gets_From_Server()
        {
            await _SignalRHelper_HubReceiverContainer_Gets_From_Server();
        }

        private  async Task _SignalRHelper_HubReceiverContainer_Gets_From_Server()
        {
            var hc = new HubConnectionBuilder()
                .WithUrl("http://localhost:19001/testhub").Build();
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
                .WithUrl("http://localhost:19001/testhub").Build();
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
                .WithUrl("http://localhost:19001/testhub").Build();
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


        public void Dispose()
        {
            _host.StopAsync().Wait();
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