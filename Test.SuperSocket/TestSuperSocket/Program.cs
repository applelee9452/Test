using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using SuperSocket.Kestrel;
using SuperSocket.ProtoBase;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Host;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace TestSuperSocket;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host_builder = Host.CreateDefaultBuilder(args)
            .ConfigureLogging((context, logging) =>
            {
                logging.AddConsole().SetMinimumLevel(LogLevel.Trace);
            });

        host_builder = host_builder
            .UseOrleans(silo_builder =>
            {
                silo_builder
                    .UseLocalhostClustering()
                    .ConfigureLogging(logging => logging.AddConsole())
                    .UseTransactions()
                    .AddMemoryGrainStorageAsDefault()
                    .AddMemoryGrainStorage("PubSubStore")
                    .AddMemoryStreams("StreamProvider");
            });

        var supersocket_builder = host_builder
            .AsSuperSocketHostBuilder<TextPackageInfo, LinePipelineFilter>()
            .ConfigureSuperSocket(options =>
            {
                options.Name = "SuperSocketServer";
                options.Listeners = new List<ListenOptions>
                {
                    new ListenOptions
                    {
                        Ip = "Any",
                        Port = 5010,
                        ConnectionAcceptTimeOut = TimeSpan.FromSeconds(60),
                        BackLog = 100000,// 10w，服务器端连接请求队列的最大长度（即 “未完成三次握手的连接” 的临时缓冲区大小）
                    }
                };
            })
            .UseSession<SuperSocketSession>()
            .UsePackageHandler(
                (session, packet) =>
                {
                    var s = (SuperSocketSession)session;
                    return s.OnRecvPackage(packet);
                },
                (session, h) =>
                {
                    var s = (SuperSocketSession)session;
                    return s.OnRecvError(h);
                }
            )
            .UseHostedService<SuperSocketServerHostedService>()
            .UseKestrelPipeConnection();

        var webhost_builder = supersocket_builder
            .ConfigureWebHostDefaults(web_builder =>
            {
                web_builder
                    .UseKestrel(options =>
                    {
                        options.Limits.MaxConcurrentConnections = 100000;
                        options.Limits.MaxConcurrentUpgradedConnections = 100000;
                        options.Listen(IPAddress.Any, 5000);
                    })
                    .ConfigureServices((context, services) =>
                    {
                        services.AddControllers();
                    })
                    .Configure((context, app) =>
                    {
                        if (context.HostingEnvironment.IsDevelopment())
                        {
                            app.UseDeveloperExceptionPage();
                        }
                        else
                        {
                            app.UseExceptionHandler("/Error");
                            app.UseHsts();
                        }

                        app.UseRouting();
                        app.UseAuthorization();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
            });

        var host = webhost_builder.Build();

        await host.RunAsync();
    }
}
