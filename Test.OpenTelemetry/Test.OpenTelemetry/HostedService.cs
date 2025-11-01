using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;

namespace DEF;

public static class TcpMetrics
{
    public const string ActiveConnections = "tcp.active_connections";
    public const string ConnectionsEstablished = "tcp.connections.established";
    public const string ConnectionsClosed = "tcp.connections.closed";
    public const string BytesReceived = "tcp.bytes.received";
    public const string ConnectionsRefused = "tcp.connections.refused";
}

public class HostedService : IHostedService
{
    ILogger Logger { get; set; }
    Meter Meter { get; set; }
    ObservableGauge<int> ActiveConnectionsGauge { get; set; }
    Counter<long> ConnectionsEstablishedCounter { get; set; }
    Counter<long> ConnectionsClosedCounter { get; set; }
    Counter<long> BytesReceivedCounter { get; set; }
    Counter<long> ConnectionsRefusedCounter { get; set; }

    ActivitySource _activitySource;

    private int _activeConnections;

    private CancellationTokenSource _cts;
    private PeriodicTimer _timer;
    private Task _timerTask;
    private const int TimerIntervalSeconds = 10;

    public HostedService(ILogger<HostedService> logger, ActivitySource activitySource, Meter m)
    {
        Logger = logger;
        _activitySource = activitySource;
        Meter = m;

        ActiveConnectionsGauge = Meter.CreateObservableGauge<int>(
            name: TcpMetrics.ActiveConnections,
            observeValue: () => _activeConnections,
            unit: "connections",
            description: "当前活跃的连接数"
        );

        ConnectionsEstablishedCounter = Meter.CreateCounter<long>(
            name: TcpMetrics.ConnectionsEstablished,
            unit: "connections",
            description: "累计建立的连接数"
        );

        ConnectionsClosedCounter = Meter.CreateCounter<long>(
            name: TcpMetrics.ConnectionsClosed,
            unit: "connections",
            description: "累计关闭的连接数"
        );

        BytesReceivedCounter = Meter.CreateCounter<long>(
            name: TcpMetrics.BytesReceived,
            unit: "bytes",
            description: "累计接收的字节数"
        );

        ConnectionsRefusedCounter = Meter.CreateCounter<long>(
            name: TcpMetrics.ConnectionsRefused,
            unit: "connections",
            description: "累计被拒绝的TCP连接数"
        );
    }

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("StartAsync()");

        await Task.Delay(1);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(TimerIntervalSeconds));
        _timerTask = RunTimerAsync(_cts.Token);
    }

    async Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        if (_timerTask != null)
        {
            _cts.Cancel();

            await _timerTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        }

        Logger.LogInformation("StopAsync()");
    }

    private async Task RunTimerAsync(CancellationToken cancellation_token)
    {
        try
        {
            while (await _timer!.WaitForNextTickAsync(cancellation_token) && !cancellation_token.IsCancellationRequested)
            {
                Logger.LogInformation("111111111111111111111");

                try
                {
                    int n = Random.Shared.Next(100, 200);
                    Interlocked.Exchange(ref _activeConnections, n);

                    // 1. 创建一个Span（使用ActivitySource.StartActivity）
                    // 第一个参数：Span名称（描述当前操作，如"处理TCP消息"）
                    // 第二个参数：Span类型（Server/Client/Internal等，根据场景选择）
                    using (var activity = _activitySource.StartActivity("处理TCP消息", ActivityKind.Server))
                    {
                        if (activity == null) return; // 若追踪被禁用，Activity可能为null

                        // 2. 为Span添加标签（Key-Value形式，用于筛选和分析）
                        activity.SetTag("tcp.client.ip", "192.168.1.100"); // 客户端IP
                        activity.SetTag("message.type", "heartbeat"); // 消息类型
                        activity.SetTag("message.size", 1024); // 消息大小

                        // 3. 添加事件（记录关键时间点）
                        activity.AddEvent(new ActivityEvent("开始处理消息"));

                        try
                        {
                            // 模拟业务逻辑
                            Thread.Sleep(100);

                            // 4. 嵌套子Span（若有子操作，如调用其他服务）
                            using (var childActivity = _activitySource.StartActivity("验证消息格式", ActivityKind.Internal))
                            {
                                childActivity?.SetTag("validation.passed", true);
                                Thread.Sleep(50);
                            }

                            // 5. 标记Span成功
                            activity.SetStatus(ActivityStatusCode.Ok);
                            activity.AddEvent(new ActivityEvent("消息处理完成"));
                        }
                        catch (Exception ex)
                        {
                            // 6. 若出错，标记Span失败并记录异常
                            activity.SetStatus(ActivityStatusCode.Error);
                            activity.AddException(ex); // 记录异常详情
                            activity.AddEvent(new ActivityEvent("消息处理失败"));
                            Logger.LogError(ex, "处理消息出错");
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Logger.LogError(ex, "定时器回调执行失败（已捕获，不影响后续执行）");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("定时器任务已被正常取消");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "定时器循环发生致命错误，已终止");
        }
    }
}
