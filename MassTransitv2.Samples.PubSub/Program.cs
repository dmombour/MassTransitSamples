using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.BusConfigurators;
using System.Diagnostics;

namespace MassTransitv2.Samples.PubSub
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
            
            IServiceBus bus = null;

            if (input.ToLower().StartsWith("s"))
            {
                //Create the service bus as a subscriber. Wire up the subscription post bus setup
                bus = CreateBus(uniqueProcessName + "TestSubscriber", x =>
                {
                    var consumeLimit = System.Environment.ProcessorCount * 4;
                    x.SetConcurrentConsumerLimit(consumeLimit);
                    x.Subscribe(subs =>
                    {
                        subs.Consumer<SomethingHappenedConsumer>().Permanent();
                    });
                });

                do
                {
                    input = Console.ReadLine();

                } while (!input.StartsWith("q"));

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
                        bus.Publish<ISomethingHappened>(message, x => { x.SetDeliveryMode(MassTransit.DeliveryMode.Persistent); });
                    });                    
                }
            }            

            bus.Dispose();
        }

        public static IServiceBus CreateBus(string queueName, Action<ServiceBusConfigurator> moreInitialization)
        {
            IServiceBus bus = null;
            Console.WriteLine("Use (r)abbitMQ or (m)msmq?");
            var input = Console.ReadLine();

            var server = System.Net.Dns.GetHostName();
            server = "localhost";

            if (input.ToLower().StartsWith("m"))
            {
                bus = ServiceBusFactory.New(x =>
                {
                    x.UseMsmq(y =>
                        {
                            y.VerifyMsmqConfiguration();
                            y.UseMulticastSubscriptionClient();
                            
                        });
                    x.ReceiveFrom("msmq://" + server + "/MtPubSubExample_" + queueName);

                    if (moreInitialization != null)
                    {
                        moreInitialization(x);
                    }

                });

                Console.WriteLine("msmq bus ready.");
            }
            else if (input.ToLower().StartsWith("r"))
            {
                bus = ServiceBusFactory.New(x =>
                {
                    x.UseRabbitMq();

                    var rabbitPreFetchCount = System.Environment.ProcessorCount * 4;
                    var address = string.Format("rabbitmq://{0}/MtPubSubExample_{1}?prefetch={2}", server, queueName, rabbitPreFetchCount);
                    x.ReceiveFrom(address);

                    if (moreInitialization != null)
                    {
                        moreInitialization(x);
                    }

                });

                Console.WriteLine("rabbitMQ bus ready");
            }

            return bus;
        }
    }
}
