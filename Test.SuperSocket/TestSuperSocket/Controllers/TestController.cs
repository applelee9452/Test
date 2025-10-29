using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Threading.Tasks;

namespace TestSuperSocket.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        ILogger Logger { get; set; }
        IGrainFactory GrainFactory { get; set; }
        Random Rd { get; set; } = new();

        public TestController(ILogger<TestController> logger, IGrainFactory grainFactory)
        {
            Logger = logger;
            GrainFactory = grainFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            int n = Rd.Next(0, 10000);

            var grain_hello = GrainFactory.GetGrain<IGrainHello>("1");

            var response = await grain_hello.Notify2Session(n.ToString());

            return Ok($"{response}");
        }
    }
}
