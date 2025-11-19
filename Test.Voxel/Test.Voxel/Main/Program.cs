using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Threading.Tasks;

namespace Test;

internal class Program
{
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Async(a => a.File(path: "Logs/Test.log",
                          rollingInterval: RollingInterval.Day,
                          formatter: new Serilog.Formatting.Json.JsonFormatter()))
            .WriteTo.Async(a => a.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"))
            .CreateLogger();

        Log.Information("应用程序启动");

        var host = Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<HostedServiceA>();
            })
            .Build();

        await host.RunAsync();

        Log.Information("应用程序结束");
    }
}