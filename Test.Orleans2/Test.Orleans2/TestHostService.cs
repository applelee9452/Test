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

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        IGrainTest grain_test = GrainFactory.GetGrain<IGrainTest>("Test");
        await grain_test.Test();

        //await Task.Delay(1);
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
