using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
}

public class GroupHub : Hub<IFoo>, IBar
    {
        public static ConcurrentDictionary<string,int> callCounter = new ConcurrentDictionary<string, int>();
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

        public Task MultiParam(string nerp, Guid guid)
        {
            callCounter.AddOrUpdate(nerp + guid.ToString(), n => 1,
                (n, i) => i + 1);
            this.Clients.Caller.MultiParam(nerp, guid);
            return Task.CompletedTask;
        }
    }
}
