using Orleans;
using System.Threading.Tasks;

namespace Test;

public interface IGrainA : IGrainWithStringKey
{
    Task AAA();

    Task AAA2();
}
