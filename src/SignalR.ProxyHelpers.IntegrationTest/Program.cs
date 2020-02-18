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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    
                    webBuilder.UseStartup<Startup>();
                });        
    }
}