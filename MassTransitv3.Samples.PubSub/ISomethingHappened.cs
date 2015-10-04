using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassTransitv3.Samples.PubSub
{
    public interface ISomethingHappened
    {
        string Who { get; }
        string What { get; }
        DateTime When { get; }
        int HowMuch { get; }
    }
}
