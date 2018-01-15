using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

#pragma warning disable CS1591

namespace AarekhLabs.Memcached.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .CaptureStartupErrors(true)
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseKestrel()
                .Build();
    }
}

#pragma warning restore CS1591

