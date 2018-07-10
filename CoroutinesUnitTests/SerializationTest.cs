using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Coroutines.Tests
{
    public class SerializationTest
    {
        public class SerializedCoroutine : Coroutine
        {
            public int Iteration { get; set; } = 0;

            protected override IEnumerator<IWaitObject> Execute()
            {
                Iteration = 1;
                yield return null;
                Iteration = 2;
            }
        }

        [Fact]
        public void SerializeSchedulerWithCoroutine_StartingExecution()
        {
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            };

            string data;
            {
                var eventQueue = new EventQueue();
                var scheduler = new EventCoroutineScheduler(eventQueue);
                var coroutine = new SerializedCoroutine();

                scheduler.Execute(coroutine);


                data = JsonConvert.SerializeObject(scheduler, jsonSerializerSettings);
            }

            {
                var newScheduler = JsonConvert.DeserializeObject<EventCoroutineScheduler>(data, jsonSerializerSettings);

                var newEventQueue = (EventQueue)newScheduler.EventQueue;
                newScheduler.Update(newEventQueue.DequeueEvent());
                newScheduler.NewFrame(0);
                newEventQueue.NewFrame();
                newScheduler.Update(newEventQueue.DequeueEvent());
            }
        }

        [Fact]
        public void SerializeSchedulerWithCoroutine_InExecution()
        {
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            };

            string data;
            {
                var eventQueue = new EventQueue();
                var scheduler = new EventCoroutineScheduler(eventQueue);
                var coroutine = new SerializedCoroutine();

                scheduler.Execute(coroutine);
                scheduler.Update(eventQueue.DequeueEvent());

                data = JsonConvert.SerializeObject(scheduler, jsonSerializerSettings);
            }

            {
                var newScheduler = JsonConvert.DeserializeObject<EventCoroutineScheduler>(data, jsonSerializerSettings);

                var newEventQueue = (EventQueue)newScheduler.EventQueue;
                newScheduler.NewFrame(0);
                newEventQueue.NewFrame();
                newScheduler.Update(newEventQueue.DequeueEvent());
            }
        }

    }
}
