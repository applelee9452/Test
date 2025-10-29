using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class RobotOptions
{
    public int Count { get; set; } = 30000;
    public int IntervalMs { get; set; } = 100;
}

public class RobotManager : IHostedService
{
    private readonly int _robotCount;
    private readonly TimeSpan _interval;
    private readonly ILogger<RobotManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private List<Robot> _robots;
    private CancellationTokenSource _cts;

    public RobotManager(
        IOptions<RobotOptions> options,
        ILogger<RobotManager> logger,
        IServiceProvider serviceProvider)
    {
        _robotCount = options.Value.Count;
        _interval = TimeSpan.FromMilliseconds(options.Value.IntervalMs);
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _robots = new List<Robot>(_robotCount);

        _logger.LogInformation("开始创建 {Count} 个Bot...", _robotCount);

        for (int i = 0; i < _robotCount; i++)
        {
            // 每个机器人创建独立的服务作用域（确保日志等依赖隔离）
            using var scope = _serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Robot>>();
            var robot = new Robot(i + 1, _interval, logger);
            _robots.Add(robot);
        }

        _logger.LogInformation("{Count}个Bot创建完成，开始启动...", _robotCount);

        var startTasks = _robots.Select(robot =>
            robot.StartAsync(_cts.Token)
        );

        await Task.WhenAll(startTasks);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_robots == null) return;

        _logger.LogInformation("开始停止 {Count} 个Bot...", _robotCount);

        foreach (var robot in _robots)
        {
            robot.Stop();
        }

        var disposeTasks = _robots.Select(robot =>
            robot.DisposeAsync().AsTask()
        );
        await Task.WhenAll(disposeTasks);

        _cts?.Cancel();
        _cts?.Dispose();
        _logger.LogInformation("{Count} 个Bot已全部停止", _robotCount);
    }
}