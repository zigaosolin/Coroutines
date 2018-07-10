using Coroutines;
using System;

namespace Reactors.Performance
{
    class Program
    {
        static void Main(string[] args)
        {
            SimpleEventsProcessTest();
            CoroutineEventsProcessTest();
            CoroutineUpdateTest();
            CoroutineWaitTriggersTest();
            CoroutinePollTimersTest();
        }

        private static void SimpleEventsProcessTest()
        {
            const int EventsNum = 10000000;
            var reactor = new SimpleReactor();
            for (int i = 0; i < EventsNum; i++)
            {
                reactor.Enqueue(null, new SimpleEvent());
            }

            DateTime start = DateTime.UtcNow;
            reactor.Update(0);
            DateTime end = DateTime.UtcNow;

            Console.WriteLine($"Time to process {EventsNum:n0} events: {(end - start).TotalSeconds} s");
        }

        private static void CoroutineEventsProcessTest()
        {
            const int EventsNum = 1000000;
            var reactor = new CoroutineResponseReactor();
            for (int i = 0; i < EventsNum; i++)
            {
                reactor.Enqueue(null, new SimpleEvent());
            }

            DateTime start = DateTime.UtcNow;
            while (reactor.EventsProcessed < EventsNum)
            {
                // We start 100 coroutines per frame here
                reactor.Update(0, 100);
            }
            DateTime end = DateTime.UtcNow;

            Console.WriteLine($"Time to process {EventsNum:n0} coroutine events (two frame events): {(end - start).TotalSeconds} s");
        }

        private static void CoroutineUpdateTest()
        {
            const int LoopsNum = 10000000;
            {
                var scheduler = new InterleavedCoroutineScheduler();
                var coroutine = new SimpleCoroutine(LoopsNum);

                scheduler.Execute(coroutine);

                DateTime start = DateTime.UtcNow;
                while (coroutine.Status != CoroutineStatus.CompletedNormal)
                {
                    scheduler.Update(0);
                }
                DateTime end = DateTime.UtcNow;

                Console.WriteLine($"Time to process {LoopsNum:n0} coroutine updates by interleved scheduler: {(end - start).TotalSeconds} s");
            }

            {
                var eventQueue = new EventQueue();
                var scheduler = new EventCoroutineScheduler(eventQueue);
                var coroutine = new SimpleCoroutine(LoopsNum);

                scheduler.Execute(coroutine);

                DateTime start = DateTime.UtcNow;
                while (true)
                {
                    if (!eventQueue.TryDequeue(out IEvent ev))
                        break;

                    scheduler.Update((ICoroutineEvent)ev);
                    scheduler.NewFrame(0);
                    eventQueue.NewFrame();
                }
                DateTime end = DateTime.UtcNow;

                Console.WriteLine($"Time to process {LoopsNum:n0} coroutine updates by event scheduler: {(end - start).TotalSeconds} s");
            }
        }

        private static void CoroutineWaitTriggersTest()
        {
            const int WaitNum = 1000000;
            const float AverageWaitTime = 1000;

            var eventQueue = new EventQueue();
            var scheduler = new EventCoroutineScheduler(eventQueue);
            var coroutine = new SimpleCoroutine(WaitNum);

            scheduler.Execute(coroutine);

            var random = new Random(23132);

            DateTime start = DateTime.UtcNow;
            int coroutinesSpawned = 0;
            int coroutinesUpdated = 0;
            while (true)
            {
                while (true)
                {
                    if (!eventQueue.TryDequeue(out IEvent ev))
                        break;

                    coroutinesUpdated++;
                    scheduler.Update((ICoroutineEvent)ev);
                }

                scheduler.NewFrame(1f);
                eventQueue.NewFrame();


                // Enqueue new coroutine
                if (coroutinesSpawned < WaitNum)
                {
                    ++coroutinesSpawned;
                    scheduler.Execute(
                        new WaitForSecondsCoroutine((float)random.NextDouble() * 2.0f * AverageWaitTime)
                    );
                } else
                {
                    // Each coroutine is two event updates
                    if(coroutinesUpdated >= WaitNum * 2)
                        break;
                }
            }
            DateTime end = DateTime.UtcNow;

            Console.WriteLine($"Time to process {WaitNum:n0} coroutine with average wait time {AverageWaitTime:n} by event scheduler: {(end - start).TotalSeconds} s");

        }

        private static void CoroutinePollTimersTest()
        {
            const int WaitNum = 10000;
            const float AverageWaitTime = 1000;

            var eventQueue = new EventQueue();
            var scheduler = new EventCoroutineScheduler(eventQueue);
            var coroutine = new SimpleCoroutine(WaitNum);

            scheduler.Execute(coroutine);

            var random = new Random(23132);

            DateTime start = DateTime.UtcNow;
            int coroutinesSpawned = 0;
            int coroutinesUpdated = 0;
            while (true)
            {
                while (true)
                {
                    if (!eventQueue.TryDequeue(out IEvent ev))
                        break;

                    coroutinesUpdated++;
                    scheduler.Update((ICoroutineEvent)ev);
                }

                scheduler.NewFrame(1f);
                eventQueue.NewFrame();


                // Enqueue new coroutine
                if (coroutinesSpawned < WaitNum)
                {
                    ++coroutinesSpawned;
                    scheduler.Execute(
                        new PollWaitForSecondsCoroutine((float)random.NextDouble() * 2.0f * AverageWaitTime)
                    );
                }
                else
                {
                    // This timing is not correct as "A LOT" of coroutines are still not finished but it shows the differenct
                    break;
                }
            }
            DateTime end = DateTime.UtcNow;

            Console.WriteLine($"Time to process {WaitNum:n0} coroutine with average wait time {AverageWaitTime:n} by event scheduler: {(end - start).TotalSeconds} s");

        }
    }
}
