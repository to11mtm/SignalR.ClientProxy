using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SignalR.ProxyHelpers.IntegrationTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            TestHostbuilder.CreateHostBuilder(args).Build().Run();
        }

    }

    public static class TestHostbuilder
    {

        public static IHostBuilder CreateHostBuilder(string[] args, int? port = null) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHost(x =>
                {
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(k =>
                    {
                        if (port != null)
                        {
                            k.ListenLocalhost(port.Value);
                        }
                    });
                    webBuilder.UseStartup<Startup>();
                });        
    }
}