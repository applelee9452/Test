using Orleans;
using System.Threading;
using System.Threading.Tasks;

namespace Test;

public interface IGrainBot : IGrainWithStringKey
{
    Task AAA();

    Task AAA2(CancellationToken cacellation_token);
}
