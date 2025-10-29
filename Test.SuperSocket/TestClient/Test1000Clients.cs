using SuperSocket.Client;
using SuperSocket.Connection;
using SuperSocket.ProtoBase;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace TestClient;

public class Test1000Clients
{
    List<IEasyClient<TextPackageInfo>> ListClient { get; set; } = new();

    public async Task Run()
    {
        int client_count = 1000;

        ConnectionOptions options = new()
        {
        };

        for (int i = 0; i < client_count; i++)
        {
            var client = new EasyClient<TextPackageInfo>(new LinePipelineFilter(), options).AsClient();
            ListClient.Add(client);
        }

        List<Task<bool>> list_task = new(client_count);
        foreach (var i in ListClient)
        {
            var t = i.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 5010));
            list_task.Add(t.AsTask());
        }
        await Task.WhenAll(list_task);

        int connect_count = 0;
        foreach (var i in list_task)
        {
            if (i.Result) connect_count++;
        }
        Console.WriteLine($"成功：{connect_count}");
        Console.WriteLine($"失败：{list_task.Count - connect_count}");

        //List<Task> list_task2 = new(client_count);
        //foreach (var i in ListClient)
        //{
        //    string s = $"aaaaaaaaaaa\r\n";
        //    var d = System.Text.Encoding.UTF8.GetBytes(s);
        //    var t = i.SendAsync(d);

        //    list_task2.Add(t.AsTask());
        //}
        //await Task.WhenAll(list_task2);

        //foreach (var i in ListClient)
        //{
        //    i.StartReceive();
        //}

        await Task.Delay(600000);
    }
}
