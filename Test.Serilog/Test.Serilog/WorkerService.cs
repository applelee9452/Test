using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test;

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

            // 每5秒执行一次
            await Task.Delay(3000, stoppingToken);
        }

        _logger.LogInformation("工作服务正在停止");
    }
}
