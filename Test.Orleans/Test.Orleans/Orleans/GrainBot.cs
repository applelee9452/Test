using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Test;

public class GrainBot : Grain, IGrainBot
{
    ILogger Logger { get; set; }
    IHttpClientFactory HttpClientFactory { get; set; }

    public GrainBot(ILogger<GrainBotMgr> logger, IHttpClientFactory http_client_factory)
    {
        Logger = logger;
        HttpClientFactory = http_client_factory;
    }

    Task IGrainBot.AAA()
    {
        //Logger.LogInformation("GrainA.AAA() Id={Id}", this.GetPrimaryKeyString());

        return Task.CompletedTask;
    }

    async Task IGrainBot.AAA2(CancellationToken cacellation_token)
    {
        int a = 1;
        int b = a + 99;

        using var http_client = HttpClientFactory.CreateClient();

        try
        {
            var response = await http_client.GetAsync("https://api.example.com/test", cacellation_token);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync(cacellation_token);
        }
        catch (Exception)
        {
        }

        Logger.LogInformation("GrainBot.AAA() Id={Id} ThreadId={ThreadId}",
            this.GetPrimaryKeyString(), Thread.CurrentThread.ManagedThreadId);
    }
}
