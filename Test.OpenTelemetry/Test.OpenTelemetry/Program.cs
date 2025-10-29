using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;
using System;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;

namespace DEF;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Async(a => a.File(
                path: "Logs/Test.log",
                rollingInterval: RollingInterval.Day,
                formatter: new Serilog.Formatting.Json.JsonFormatter()))
            .WriteTo.Async(a => a.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext:lj}] {Message:lj}{NewLine}{Exception}"))
            .CreateLogger();

        Log.Information("应用程序启动");

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging => logging.ClearProviders())
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                services.Configure<RobotOptions>(context.Configuration.GetSection("Robot"));

                services.AddOpenTelemetry()
                    .ConfigureResource(resource =>
                    {
                        resource.AddService(
                            serviceName: "tcp-server-dotnet9",
                            serviceVersion: "1.0.0",
                            serviceInstanceId: Environment.MachineName
                        );
                    })
                    .WithMetrics(metrics =>
                    {
                        metrics.AddMeter("TcpServer.Metrics");

                        metrics.AddReader(sp =>
                        {
                            var otlp_exporter = new OtlpMetricExporter(new OtlpExporterOptions
                            {
                                Headers = $"Authorization=Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("a@a.com:123456"))}",
                                Endpoint = new Uri("http://localhost:5080/api/default/v1/metrics"),
                                Protocol = OtlpExportProtocol.HttpProtobuf,
                                TimeoutMilliseconds = 15000
                            });

                            return new PeriodicExportingMetricReader(otlp_exporter, 1000, 3000);
                        });
                    });

                services.AddSingleton(new Meter("TcpServer.Metrics", "1.0.0"));

                services.AddHostedService<HostedService>();
            })
            .Build();

        await host.RunAsync();

        Log.CloseAndFlush();

        return 0;
    }
}