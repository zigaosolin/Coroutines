using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Xunit;
using System.Linq;

namespace Coroutines.Tests
{
    public class SerializationTest
    {

        public class MyContractResolver : DefaultContractResolver
        {
            

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                if (type == typeof(EventQueue) || type == typeof(Coroutine) ||
                   type == typeof(EventCoroutineScheduler))
                {
                    var props = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                       .Select(f => base.CreateProperty(f, memberSerialization))
                            .ToList();
                    props.ForEach(p => { p.Writable = true; p.Readable = true; });
                    return props;
                }
                else
                {
                    var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                    .Where(p => p.CanRead && p.CanWrite)
                                    .Select(p => base.CreateProperty(p, memberSerialization))
                                .Union(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                           .Select(f => base.CreateProperty(f, memberSerialization)))
                                .ToList();
                    props.ForEach(p => { p.Writable = true; p.Readable = true; });
                    return props;
                }
            }
        }

        IEnumerator<IWaitObject> SimpleEnumerator()
        {
            int a = 0;
            yield return null;
            a = 1;
        }

        class EnumeratorHolder
        {
            public IEnumerator<IWaitObject> Enumerator;
        }

        [Fact]
        public void SerializeIterator()
        {
            var enumerator = SimpleEnumerator();
            var holder = new EnumeratorHolder()
            {
                Enumerator = enumerator
            };
            Assert.True(enumerator.MoveNext());

            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                ContractResolver = new MyContractResolver()
            };

            var data = JsonConvert.SerializeObject(holder, jsonSerializerSettings);

            var newHolder = JsonConvert.DeserializeObject<EnumeratorHolder>(data, jsonSerializerSettings);
            Assert.False(newHolder.Enumerator.MoveNext());

        }

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
                TypeNameHandling = TypeNameHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ContractResolver = new MyContractResolver()
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
                TypeNameHandling = TypeNameHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ContractResolver = new MyContractResolver()
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
