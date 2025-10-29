using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Test;

public class GrainBotMgr : Grain, IGrainBotMgr
{
    ILogger Logger { get; set; }
    RobotOptions RobotOptions { get; set; }

    public GrainBotMgr(ILogger<GrainBotMgr> logger, IOptions<RobotOptions> robot_options)
    {
        Logger = logger;
        RobotOptions = robot_options.Value;
    }

    async Task IGrainBotMgr.Run(CancellationToken cacellation_token)
    {
        Logger.LogInformation("GrainBotMgr.Run");

        int num = 10;

        //--------------------------------------------
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        List<Task> list_task = new(num);
        for (int i = 0; i < num; i++)
        {
            var grain_a = GrainFactory.GetGrain<IGrainBot>(i.ToString());
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
            var grain_a = GrainFactory.GetGrain<IGrainBot>(i.ToString());
            var t = grain_a.AAA2(cacellation_token);
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

        Logger.LogInformation("GrainBotMgr.Run() 总耗时：{Seconds1}，{Seconds2}，{Seconds3}", s1, s2, s3);
    }

    void Func2()
    {
        int a = 0;
        int b = a + 100;
    }
}
