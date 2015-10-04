using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;

namespace MassTransitv3.Samples.PubSub
{
    class SomethingHappenedConsumer : IConsumer<ISomethingHappened>
    {
        static volatile int mCounter = 0;
        static DateTimeOffset mStartTime;

        public static void ResetCounter()
        {
            Interlocked.Exchange(ref mCounter, 0);
        }

        public static int GetCounter()
        {
            return mCounter;
        }

        public Task Consume(ConsumeContext<ISomethingHappened> message)
        {
            var currentCount = Interlocked.Increment(ref mCounter);

            var sb = new StringBuilder();
            sb.Append("Who: ");
            sb.Append(message.Message.Who);
            sb.Append("  What: ");
            sb.Append(message.Message.What);
            sb.Append("  When: ");
            sb.Append(message.Message.When.ToString());
            sb.Append("  Processed: #");
            sb.Append(mCounter.ToString());
            sb.Append("@");
            sb.Append(DateTime.Now.ToString());
            sb.Append(" on ");
            sb.Append(System.Environment.MachineName);
            sb.Append(" ThreadId:");
            sb.Append(Thread.CurrentThread.ManagedThreadId.ToString());

            Console.WriteLine(sb.ToString());

            var expect = message.Message.HowMuch;            
            if (currentCount == 1)
            {
                //First message received
                mStartTime = DateTimeOffset.Now;
            }

            if (expect == currentCount)
            {
                var duration = (DateTimeOffset.Now - mStartTime);
                var msgPerSecond = currentCount / duration.TotalSeconds;

                //Delay for 1 second to ensure console is flushed and this is the last message
                Console.Out.Flush();
                Thread.Sleep(1000);

                Console.WriteLine(string.Format("COMPLETE: Received {0} messages at a rate of {1} msg per sec", currentCount, msgPerSecond));
                ResetCounter();
            }

            return message.CompleteTask;
        }

    }
}
