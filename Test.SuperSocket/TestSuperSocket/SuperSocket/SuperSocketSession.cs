using Microsoft.Extensions.Logging;
using Orleans;
using SuperSocket;
using SuperSocket.ProtoBase;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions.Session;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TestSuperSocket;

public class SuperSocketSession : AppSession, IHelloObserver
{
    string ClientIp { get; set; }
    IGrainFactory GrainFactory { get; set; }

    public SuperSocketSession(IGrainFactory grain_factory)
    {
        GrainFactory = grain_factory;
    }

    protected override async ValueTask OnSessionConnectedAsync()
    {
        var ep = (IPEndPoint)RemoteEndPoint;
        ClientIp = ep.Address.MapToIPv4().ToString();

        Logger.LogInformation("OnSessionConnectedAsync, ClientIp={ClientIp}", ClientIp);

        var ob = GrainFactory.CreateObjectReference<IHelloObserver>(this);

        var grain_hello = GrainFactory.GetGrain<IGrainHello>("Hello");
        await grain_hello.Sub(ob);
    }

    protected override ValueTask OnSessionClosedAsync(SuperSocket.Connection.CloseEventArgs e)
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask OnRecvPackage(TextPackageInfo package)
    {
        //Logger.LogInformation("{Text}", package.Text);

        var grain_hello = GrainFactory.GetGrain<IGrainHello>("Hello");

        //var ob = GrainFactory.GetGrain<IHelloObserver>("Hello");

        //await grain_hello.Sub(ob);

        var response = await grain_hello.Notify2Session(package.Text);

        Logger.LogInformation(response);

        response += "\r\n";

        await ((IAppSession)this).SendAsync(Encoding.UTF8.GetBytes(response));

        //return ValueTask.CompletedTask;

        //if (OrderStatusObserverGrain != null)
        //{
        //    GrainFactory.DeleteObjectReference<IHelloObserver>(OrderStatusObserverGrain);
        //    OrderStatusObserverGrain = null;
        //}
    }

    public ValueTask<bool> OnRecvError(PackageHandlingException<TextPackageInfo> packet)
    {
        return ValueTask.FromResult(true);
    }

    Task IHelloObserver.OnOrleansNotify(string message)
    {
        Logger.LogInformation(message);

        return Task.CompletedTask;
    }
}