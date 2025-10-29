using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test;

public class GrainA : Grain, IGrainA
{
    ILogger Logger { get; set; }

    public GrainA(ILogger<GrainTest> logger)
    {
        Logger = logger;
    }

    Task IGrainA.AAA()
    {
        //Logger.LogInformation("GrainA.AAA() Id={Id}", this.GetPrimaryKeyString());

        return Task.CompletedTask;
    }

    Task IGrainA.AAA2()
    {
        int a = 1;
        int b = a + 99;

        //Logger.LogInformation("GrainA.AAA() Id={Id} ThreadId={ThreadId}",
        //    this.GetPrimaryKeyString(), Thread.CurrentThread.ManagedThreadId);

        return Task.CompletedTask;
    }
}
