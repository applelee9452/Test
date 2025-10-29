using Microsoft.Diagnostics.Runtime;
using Microsoft.Extensions.Logging;
using Orleans;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Test;

public class GrainTest : Grain, IGrainTest
{
    ILogger Logger { get; set; }

    public GrainTest(ILogger<GrainTest> logger)
    {
        Logger = logger;
    }

    async Task IGrainTest.Test()
    {
        Logger.LogInformation("GrainTest.Test");

        int num = 100 * 1000;

        //--------------------------------------------
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        List<Task> list_task = new(num);
        for (int i = 0; i < num; i++)
        {
            var grain_a = GrainFactory.GetGrain<IGrainA>(i.ToString());
            var t = grain_a.AAA();
            list_task.Add(t);
        }

        await Task.WhenAll(list_task);
        list_task.Clear();

        var s1 = stopwatch.Elapsed.TotalSeconds;
        stopwatch.Stop();

        //--------------------------------------------
        stopwatch.Reset();
        stopwatch.Restart();

        for (int i = 0; i < num; i++)
        {
            var grain_a = GrainFactory.GetGrain<IGrainA>(i.ToString());
            var t = grain_a.AAA2();
            list_task.Add(t);
        }

        await Task.WhenAll(list_task);
        list_task.Clear();

        var s2 = stopwatch.Elapsed.TotalSeconds;
        stopwatch.Stop();

        //--------------------------------------------

        stopwatch.Reset();
        stopwatch.Restart();

        for (int i = 0; i < num; i++)
        {
            Func2();
        }

        var s3 = stopwatch.Elapsed.TotalSeconds;
        stopwatch.Stop();

        Logger.LogInformation("GrainTest.Test() 总耗时：{Seconds1}，{Seconds2}，{Seconds3}", s1, s2, s3);
    }

    void Func2()
    {
        int a = 0;
        int b = a + 100;
    }
}
