using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperSocket.ProtoBase;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Connections;
using SuperSocket.Server.Abstractions.Session;
using System;
using System.Threading.Tasks;

namespace TestSuperSocket;

public class SuperSocketServerHostedService : SuperSocketService<TextPackageInfo>
{
    public SuperSocketServerHostedService(
        IServiceProvider service_provider,
        IOptions<ServerOptions> server_options,
        ILogger<SuperSocketServerHostedService> logger)
        : base(service_provider, server_options)
    {
    }

    protected override async ValueTask OnSessionConnectedAsync(IAppSession session)
    {
        await base.OnSessionConnectedAsync(session);
    }

    protected override async ValueTask OnSessionClosedAsync(IAppSession session, SuperSocket.Connection.CloseEventArgs e)
    {
        await base.OnSessionClosedAsync(session, e);
    }

    protected override ValueTask OnStartedAsync()
    {
        // 查看所有构建器
        var all_builders = ServiceProvider.GetServices<IConnectionFactoryBuilder>();
        foreach (var builder in all_builders)
        {
            Logger.LogInformation("构建器类型：{Name}", builder.GetType().FullName);
        }

        Logger.LogInformation("SuperSocket，启动成功，Port={Port}",
            Options.Listeners[0].Port);

        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnStopAsync()
    {
        Logger.LogInformation("SuperSocket，停止成功！");

        return ValueTask.CompletedTask;
    }
}
