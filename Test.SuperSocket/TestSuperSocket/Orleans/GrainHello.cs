using Microsoft.Extensions.Logging;
using Orleans;
using System.Threading.Tasks;

namespace TestSuperSocket;

public sealed class GrainHello : Grain, IGrainHello
{
    ILogger Logger { get; set; }
    IHelloObserver Ob { get; set; }

    public GrainHello(ILogger<GrainHello> logger)
    {
        Logger = logger;
    }

    Task IGrainHello.Sub(IHelloObserver ob)
    {
        Ob = ob;

        return Task.CompletedTask;
    }

    Task IGrainHello.UnSub()
    {
        Ob = null;

        return Task.CompletedTask;
    }

    async ValueTask<string> IGrainHello.Notify2Session(string greeting)
    {
        Logger.LogInformation("{Greeting}", greeting);

        if (Ob != null)
        {
            await Ob.OnOrleansNotify("bbbbbbbbbbbbbbbbbbb");
        }

        //string s = $"Hello, {greeting}!";
        string s = "ccccccccccccccccccc";
        return s;
    }
}
