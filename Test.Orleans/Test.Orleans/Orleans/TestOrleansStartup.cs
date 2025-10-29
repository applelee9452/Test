using Orleans;
using Orleans.Runtime;
using System.Threading;
using System.Threading.Tasks;

namespace Test;

internal class TestOrleansStartup : IStartupTask
{
    IGrainFactory GrainFactory { get; set; }

    public TestOrleansStartup(IGrainFactory grain_factory)
    {
        GrainFactory = grain_factory;
    }

    async Task IStartupTask.Execute(CancellationToken cancellationToken)
    {
        //IGrainTest grain_test = GrainFactory.GetGrain<IGrainTest>("Test");
        //await grain_test.Test();

        await Task.Delay(1);
    }
}
