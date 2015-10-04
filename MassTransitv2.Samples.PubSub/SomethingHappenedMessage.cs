using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassTransitv2.Samples.PubSub
{
    class SomethingHappenedMessage : ISomethingHappened
    {
        public string Who { get; set; }
        public string What { get; set; }
        public DateTime When { get; set; }
        public int HowMuch { get; set; }
    }
}
