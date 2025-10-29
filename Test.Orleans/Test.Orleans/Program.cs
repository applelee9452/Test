using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Serilog;
using System.Threading.Tasks;

namespace Test;

public class RobotOptions
{
    public int Count { get; set; }
    public int IntervalMs { get; set; }
}

public class ApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutMs { get; set; }
}

internal class Program
{
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("应用程序启动");

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // 清除默认配置源（如需完全自定义）
                // config.Sources.Clear();

                //config.AddJsonFile("configs/robot.json", optional: true, reloadOnChange: true);

                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .UseSerilog()
            .UseOrleans((context, silo_builder) =>
            {
                silo_builder
                    //.ConfigureServices(services =>
                    //{
                    //    services.Configure<RobotOptions>(context.Configuration.GetSection("Robot"));
                    //    services.Configure<ApiSettings>(context.Configuration.GetSection("ApiSettings"));
                    //})
                    .AddStartupTask<TestOrleansStartup>(ServiceLifecycleStage.Last)
                    .UseLocalhostClustering();
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<RobotOptions>(context.Configuration.GetSection("Robot"));
                services.Configure<ApiSettings>(context.Configuration.GetSection("ApiSettings"));

                services.AddHttpClient();
                services.AddHostedService<TestHostService>();
            })
            .Build();

        await host.RunAsync();

        Log.Information("应用程序结束");
    }
}
