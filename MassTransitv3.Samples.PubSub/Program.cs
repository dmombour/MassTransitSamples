using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.BusConfigurators;
using System.Diagnostics;

namespace MassTransitv3.Samples.PubSub
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Are you a (p)ublisher or (s)ubscriber. Start publisher first!");
            var input = Console.ReadLine();
            //Use a unique name so that we can run multiple copies of this on the same box.
            //Will use this for the queue name
            var uniqueProcessName = (Process.GetCurrentProcess().ProcessName + "_" + Process.GetCurrentProcess().Id.ToString()).Replace(".", "");
            
            IBusControl bus = null;

            if (input.ToLower().StartsWith("s"))
            {
                //Create the service bus as a subscriber. Wire up the subscription post bus setup
                bus = CreateBus(uniqueProcessName + "TestSubscriber", x =>
                {
                    var consumeLimit = System.Environment.ProcessorCount * 4;
                    x.UseRateLimit(consumeLimit);                    
                });

                ConnectHandle handle = bus.ConnectConsumer<SomethingHappenedConsumer>();

                do
                {
                    input = Console.ReadLine();

                } while (!input.StartsWith("q"));

                handle.Disconnect();

            }
            else if (input.ToLower().StartsWith("p"))
            {
                //Create the bus as a publisher
                bus = CreateBus(uniqueProcessName + "TestPublisher", x => { });

                while (input != "quit")
                {
                    Console.Write("Enter number of messages to generate: ");
                    input = Console.ReadLine();
                    int numMessages = 1;
                    Int32.TryParse(input, out numMessages);

                    Console.Write("Enter a message: ");
                    input = Console.ReadLine();

                    Parallel.For(0, numMessages, i =>
                    {
                        var message = new SomethingHappenedMessage() { Who = System.Environment.MachineName, What = input + "_" + i.ToString(), When = DateTime.Now, HowMuch = numMessages };
                        bus.Publish<ISomethingHappened>(message);
                    });                    
                }
            }

            bus.Stop();
        }

        public static IBusControl CreateBus(string queueName, Action<IBusFactoryConfigurator> moreInitialization)
        {
            IBusControl bus = null;
            Console.WriteLine("Use (r)abbitMQ or (a)zure?");
            var input = Console.ReadLine();

            var server = System.Net.Dns.GetHostName();
            server = "localhost";

            if (input.ToLower().StartsWith("a"))
            {
                //bus = ServiceBusFactory.New(x =>
                //{
                //    x.UseMsmq(y =>
                //        {
                //            y.VerifyMsmqConfiguration();
                //            y.UseMulticastSubscriptionClient();
                            
                //        });
                //    x.ReceiveFrom("msmq://" + server + "/MtPubSubExample_" + queueName);

                //    if (moreInitialization != null)
                //    {
                //        moreInitialization(x);
                //    }

                //});

                Console.WriteLine("msmq bus ready.");
            }
            else if (input.ToLower().StartsWith("r"))
            {
                bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    var rabbitPreFetchCount = System.Environment.ProcessorCount * 4;
                    var address = string.Format("rabbitmq://{0}/MtPubSubExample_{1}?prefetch={2}", server, queueName, rabbitPreFetchCount);
                    var host = cfg.Host(new Uri(address), h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });

                    if (moreInitialization != null)
                    {
                        moreInitialization(cfg);
                    }
                });               

                Console.WriteLine("rabbitMQ bus ready");
            }

            bus.Start();
            return bus;
        }
    }
}
