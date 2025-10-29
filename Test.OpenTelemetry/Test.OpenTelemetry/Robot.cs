using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public enum RobotStatus
{
    Idle,
    Running,
    Stopped
}

public class Robot : IAsyncDisposable
{
    private readonly int _robotId;
    private readonly TimeSpan _interval;
    private readonly ILogger<Robot> _logger;
    private readonly PeriodicTimer _timer;
    private RobotStatus _status;
    private bool _isDisposed;

    public Robot(int robotId, TimeSpan interval, ILogger<Robot> logger)
    {
        _robotId = robotId;
        _interval = interval;
        _logger = logger;
        _timer = new PeriodicTimer(interval);
        _status = RobotStatus.Idle;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_status == RobotStatus.Running)
        {
            return;
        }

        _status = RobotStatus.Running;
        //_logger.LogInformation("{Id} 启动，周期 {Interval}ms", _robotId, _interval.TotalMilliseconds);

        try
        {
            while (_status == RobotStatus.Running && await _timer.WaitForNextTickAsync(cancellationToken))
            {
                await ExecuteTaskAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            //_logger.LogInformation("{Id} 被取消", _robotId);
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "{Id} 执行任务失败", _robotId);
        }
        finally
        {
            _status = RobotStatus.Stopped;
            //_logger.LogInformation("{Id} 停止", _robotId);
        }
    }

    private Task ExecuteTaskAsync(CancellationToken cancellation_token)
    {
        var task_id = Guid.NewGuid().ToString("N").Substring(0, 8);

        //_logger.LogInformation("{RobotId}执行任务 {TaskId}...", _robotId, task_id);

        int count = 1000;
        for (int i = 0; i < count; i++)
        {
            var executionDelay = Random.Shared.Next(100, 1000);
        }

        //await Task.Delay(Random.Shared.Next(10, 100), cancellation_token);

        return Task.CompletedTask;
    }

    // 停止机器人
    public void Stop()
    {
        _status = RobotStatus.Stopped;
    }

    // 释放资源
    public ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return new ValueTask();
        }

        Stop();

        _timer.Dispose(); // 释放定时器

        _isDisposed = true;
        GC.SuppressFinalize(this);

        return new ValueTask();
    }
}