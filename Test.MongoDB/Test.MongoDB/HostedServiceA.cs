using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Test;

public class HostedServiceA : IHostedService
{
    ILogger Logger { get; set; }
    Mongo Mongo { get; set; }

    public HostedServiceA(ILogger<HostedServiceA> logger, Mongo mongo_client)
    {
        Logger = logger;
        Mongo = mongo_client;
    }

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("HostedServiceA.StartAsync()");

        await Mongo.ConnectAsync();
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("HostedServiceA.StopAsync()");

        return Task.CompletedTask;
    }
}
