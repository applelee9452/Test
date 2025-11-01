using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;

namespace DEF;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.OpenTelemetry(options =>
            {
                options.Headers["Authorization"] = $"Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("a@a.com:123456"))}";
                options.Endpoint = "http://127.0.0.1:5080/api/default";
                options.Protocol = OtlpProtocol.HttpProtobuf;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = "tcp-server-dotnet9",
                };
            })
            //.WriteTo.Async(a => a.File(
            //    path: "Logs/Test.log",
            //    rollingInterval: RollingInterval.Day,
            //    formatter: new Serilog.Formatting.Json.JsonFormatter()))
            .WriteTo.Async(a => a.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext:lj}] {Message:lj}{NewLine}{Exception}"))
            .CreateLogger();

        Log.Information("应用程序启动");

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
            })
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
                    })
                    .WithTracing(tracing =>
                    {
                        tracing.AddSource("TcpServer.Tracing");
                        //tracing.AddHttpClientInstrumentation() // 追踪HttpClient调用

                        tracing.AddOtlpExporter(options =>
                        {
                            options.Headers = $"Authorization=Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("a@a.com:123456"))}";
                            options.Endpoint = new Uri("http://localhost:5080/api/default/v1/traces");
                            options.Protocol = OtlpExportProtocol.HttpProtobuf;
                            options.TimeoutMilliseconds = 15000;// 超时设置
                        });
                    });
                //.WithLogging(logging =>
                //{
                //    logging.AddOtlpExporter(options =>
                //    {
                //        options.Headers = $"Authorization=Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("a@a.com:123456"))}";
                //        options.Endpoint = new Uri("http://localhost:5080/api/default/v1/logs");
                //        options.Protocol = OtlpExportProtocol.HttpProtobuf;
                //        options.TimeoutMilliseconds = 15000;
                //    });
                //});

                services.AddSingleton(new Meter("TcpServer.Metrics", "1.0.0"));
                services.AddSingleton(new ActivitySource("TcpServer.Tracing", "1.0.0"));

                services.AddHostedService<HostedService>();
            })
            .Build();

        await host.RunAsync();

        Log.CloseAndFlush();

        return 0;
    }
}