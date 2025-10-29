using SuperSocket.Client;
using SuperSocket.ProtoBase;
using System;
using System.Net;
using System.Threading.Tasks;

namespace TestClient;

public class TestEcho
{
    public async Task Run()
    {
        await Task.Delay(5000);

        var client = new EasyClient<TextPackageInfo>(new LinePipelineFilter()).AsClient();
        var connect_result = await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 5010));

        if (!connect_result)
        {
            Console.WriteLine("连接失败！");
            return;
        }

        while (true)
        {
            string s = $"aaaaaaaaaaa\r\n";
            var d = System.Text.Encoding.UTF8.GetBytes(s);
            await client.SendAsync(d);

            var p = await client.ReceiveAsync();

            if (p == null)
            {
                break;
            }

            Console.WriteLine($"{p.Text}");

            await Task.Delay(5000);
        }
    }
}
