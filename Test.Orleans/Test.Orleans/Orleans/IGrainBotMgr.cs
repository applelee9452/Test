using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test;

public interface IGrainBotMgr : IGrainWithStringKey
{
    Task Run(CancellationToken cacellation_token);
}
