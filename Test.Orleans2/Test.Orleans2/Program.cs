using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Serilog;
using System.Threading.Tasks;

namespace Test;
internal class Program
{
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {NewLine}{Exception}")
            .CreateLogger();

        Log.Information("应用程序启动");

        var host = Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .UseOrleans((silo_builder) =>
            {
                silo_builder
                    .AddStartupTask<TestOrleansStartup>(ServiceLifecycleStage.Last)
                    .AddSharedInMemoryStreamProvider("SharedInMemoryStreamProvider")
                    .UseLocalhostClustering();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<TestHostService>();
            })
            .Build();

        await host.RunAsync();

        Log.Information("应用程序结束");
    }
}
