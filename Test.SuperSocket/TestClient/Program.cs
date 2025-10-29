using System.Threading.Tasks;

namespace TestClient;

internal class Program
{
    public static async Task Main(string[] args)
    {
        //Test1000Clients test = new();
        TestEcho test = new();

        await test.Run();
    }
}
