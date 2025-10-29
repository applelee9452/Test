using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using Serilog;
using Serilog.Enrichers.Span;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TestOpenTelemetry
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithSpan()// 从OpenTelemetry获取TraceId和SpanId
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} TraceId={TraceId} SpanId={SpanId}{NewLine}{Exception}")
                //.WriteTo.Console(new CompactJsonFormatter())
                .WriteTo.File(path: "Logs/Test.log",
                              rollingInterval: RollingInterval.Day,
                              formatter: new Serilog.Formatting.Json.JsonFormatter())
                //outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} TraceId={TraceId} SpanId={SpanId}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("应用程序启动");

            var host = Host.CreateDefaultBuilder(args)
                .UseSerilog()// 替换默认日志系统为Serilog
                .ConfigureServices((context, services) =>
                {
                    // 3. 配置OpenTelemetry
                    services.AddOpenTelemetry()
                        .ConfigureResource(resource => resource
                            .AddService(
                                serviceName: "GenericHostDemoService",
                                serviceVersion: "1.0.0",
                                serviceInstanceId: Guid.NewGuid().ToString()))
                        .WithTracing(tracing => tracing
                            //.AddHttpClientInstrumentation() // 追踪HttpClient调用
                            .AddSource("DemoApp.ActivitySource"));
                    //.AddJaegerExporter(exporter =>
                    //{
                    //    exporter.AgentHost = "localhost";
                    //    exporter.AgentPort = 6831;
                    //}))

                    // 4. 注册服务
                    //services.AddHttpClient();
                    services.AddHostedService<WorkerService>();
                })
                .Build();

            await host.RunAsync();

            Log.Information("应用程序结束");
        }
    }

    // 6. 后台工作服务
    public class WorkerService : BackgroundService
    {
        private readonly ILogger<WorkerService> _logger;
        private static readonly ActivitySource _activitySource = new ActivitySource("DemoApp.ActivitySource");
        private int _executionCount = 0;

        public WorkerService(ILogger<WorkerService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("工作服务已启动，等待停止信号...");

            while (!stoppingToken.IsCancellationRequested)
            {
                _executionCount++;

                // 创建自定义活动用于追踪
                using (var activity = _activitySource.StartActivity("WorkerExecution"))
                {
                    activity?.SetTag("execution.count", _executionCount);
                    activity?.SetTag("execution.time", DateTime.UtcNow);

                    _logger.LogInformation("执行工作任务 - 第 {Count} 次", _executionCount);
                }

                // 每5秒执行一次
                await Task.Delay(3000, stoppingToken);
            }

            _logger.LogInformation("工作服务正在停止");
        }
    }
}
