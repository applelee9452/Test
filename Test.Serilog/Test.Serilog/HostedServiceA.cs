using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test;

public class HostedServiceA : IHostedService
{
    ILogger Logger { get; set; }

    public HostedServiceA(ILogger<HostedServiceA> logger)
    {
        Logger = logger;
    }

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("HostedServiceA.StartAsync()");

        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("HostedServiceA.StopAsync()");

        return Task.CompletedTask;
    }
}
