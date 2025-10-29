using Orleans;
using System.Threading.Tasks;

namespace TestSuperSocket;

public interface IGrainHello : IGrainWithStringKey
{
    Task Sub(IHelloObserver ob);

    Task UnSub();

    ValueTask<string> Notify2Session(string greeting);
}
