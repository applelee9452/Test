using Orleans;
using System.Threading.Tasks;

namespace TestSuperSocket;

public interface IHelloObserver : IGrainObserver
{
    Task OnOrleansNotify(string message);
}
