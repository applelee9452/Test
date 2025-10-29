using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
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

    private int _activeConnections;

    private CancellationTokenSource _cts;
    private PeriodicTimer _timer;
    private Task _timerTask;
    private const int TimerIntervalSeconds = 10;

    public HostedService(ILogger<HostedService> logger, Meter m)
    {
        Logger = logger;
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
                try
                {
                    int n = Random.Shared.Next(100, 200);
                    Interlocked.Exchange(ref _activeConnections, n);
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
