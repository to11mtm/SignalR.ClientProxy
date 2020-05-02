using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GlutenFree.SignalR.ClientProxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SignalR.ProxyHelpers.IntegrationTest
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            
            
            //using (app.ApplicationServices.CreateScope().ServiceProvider.
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                
                endpoints.MapHub<GroupHub>("/testhub");
                endpoints.MapHub<GroupHub>("/expressionTestHub");
                endpoints.MapGet("/",
                    async context =>
                    {
                        await context.Response.WriteAsync("Hello World!");
                    });
            });
        }
    }

    public class EndPt
    {
        public void Message(string msg)
        {
            
        }
    }

    

    public interface IFoo
    {
        Task StringType(string derp);
        Task ValueType(Guid val);
        Task MultiParam(string nerp, Guid guid);
    }

public interface IBar
{
    Task StringType(string nerp);
    Task ValueType(Guid guid);
    Task MultiParam(string nerp, Guid guid);
    Task<string> TestTwoWay(string nerp);
    
    string TestTwoWaySync(string nerp);
    void VoidValueType(Guid guid);
    IAsyncEnumerable<int> StreamFromServer(string nerp);
    
    Task<IAsyncEnumerable<int>> StreamFromServerAsync(string nerp);
    Task StreamToServer(IAsyncEnumerable<int> input, string nerp);

    [AlwaysInvoke]
    Task StreamToServerInvoke(IAsyncEnumerable<int> input, string nerp);

    [AlwaysSend]
    Task StreamToServerSend(IAsyncEnumerable<int> input, string nerp);

    ChannelReader<int> ReadChannel();
    
    Task<ChannelReader<int>> ReadChannelTask();
}

public class GroupHub : Hub<IFoo>, IBar
    {
        public static ConcurrentDictionary<string,int> callCounter = new ConcurrentDictionary<string, int>();
        public static ConcurrentDictionary<string,List<int>> streamCounter = new ConcurrentDictionary<string, List<int>>(); 
        public GroupHub(IServiceProvider serviceProvider)
        {
        }


        public Task StringType(string nerp)
        {
            callCounter.AddOrUpdate(nerp, n => 1, (n, i) => i + 1);
            this.Clients.Caller.StringType(nerp);
            return Task.CompletedTask;
        }

        public Task ValueType(Guid guid)
        {
            callCounter.AddOrUpdate(guid.ToString(), n => 1, (n, i) => i + 1);
            this.Clients.Caller.ValueType(guid);
            return Task.CompletedTask;
        }
        
        public void VoidValueType(Guid guid)
        {
            callCounter.AddOrUpdate(guid.ToString(), n => 1, (n, i) => i + 1);
            this.Clients.Caller.ValueType(guid);
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

        public IAsyncEnumerable<int> StreamFromServer(string nerp)
        {
            return clientStreamData();
        }

        public async Task<IAsyncEnumerable<int>> StreamFromServerAsync(string nerp)
        {
            return clientStreamData();
        }

        public async Task StreamToServer(IAsyncEnumerable<int> input, string nerp)
        {
            await foreach (var num in input)
            {
                streamCounter.AddOrUpdate(nerp, r => new List<int>() {num},
                    (r, l) =>
                    {
                        l.Add(num);
                        return l;
                    });
            }
        }
        public async Task StreamToServerInvoke(IAsyncEnumerable<int> input, string nerp)
        {
            await foreach (var num in input)
            {
                streamCounter.AddOrUpdate(nerp, r => new List<int>() {num},
                    (r, l) =>
                    {
                        l.Add(num);
                        return l;
                    });
            }
        }
        public async Task StreamToServerSend(IAsyncEnumerable<int> input, string nerp)
        {
            await foreach (var num in input)
            {
                streamCounter.AddOrUpdate(nerp, r => new List<int>() {num},
                    (r, l) =>
                    {
                        l.Add(num);
                        return l;
                    });
            }
        }

        public ChannelReader<int> ReadChannel()
        {
            var chan = Channel.CreateUnbounded<int>();

            _ = WriteChanAsync(chan);
                return chan.Reader;
        }

        public async Task<ChannelReader<int>> ReadChannelTask()
        {
            var chan = Channel.CreateUnbounded<int>();

            _ = WriteChanAsync(chan);
            return chan.Reader;
        }
        private async Task WriteChanAsync(Channel<int> chan)
        {
            for (var i = 0; i < 5; i++)
            {
                await chan.Writer.WriteAsync(i);
                await Task.Delay(TimeSpan.FromSeconds(0.25));
            }
            chan.Writer.Complete();
        }

        public Task<string> TestTwoWay(string nerp)
        {
            return Task.FromResult(nerp + "derp");
        }
        public string TestTwoWaySync(string nerp)
        {
            SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(0.5));
            return nerp + "derp";
        }

        public Task MultiParam(string nerp, Guid guid)
        {
            callCounter.AddOrUpdate(nerp + guid.ToString(), n => 1,
                (n, i) => i + 1);
            this.Clients.Caller.MultiParam(nerp, guid);
            return Task.CompletedTask;
        }
    }
}
