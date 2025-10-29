using Microsoft.Extensions.Hosting;
using Orleans;
using System.Threading;
using System.Threading.Tasks;

namespace Test;

internal class TestHostService : IHostedService
{
    IGrainFactory GrainFactory { get; set; }

    public TestHostService(IGrainFactory grain_factory)
    {
        GrainFactory = grain_factory;
    }

    async Task IHostedService.StartAsync(CancellationToken cancellation_token)
    {
        IGrainBotMgr grain_test = GrainFactory.GetGrain<IGrainBotMgr>("Test");
        await grain_test.Run(cancellation_token);
    }

    Task IHostedService.StopAsync(CancellationToken cancellation_token)
    {
        return Task.CompletedTask;
    }
}
