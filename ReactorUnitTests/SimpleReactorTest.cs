using Coroutines;
using Reactors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Reactors.Tests
{
    public class SimpleReactorTest
    {
        class Event1 : IReactorEvent
        {
            public IReactorReference Source => null;
            public string Data { get; }

            public Event1(string data)
            {
                Data = data;
            }
        }

        class Reactor1 : Reactor
        {
            public string Data { get; private set; }

            protected override void OnEvent(IReactorEvent ev)
            {
                switch(ev)
                {
                    case Event1 ev1:
                        Data = ev1.Data;
                        break;
                    default:
                        throw new Exception("Invalid event");
                }
            }
        }

        [Fact]
        public void ProcessEvent()
        {
            var reactor = new Reactor1();

            reactor.Update(0.1f);
            Assert.Null(reactor.Data);
            reactor.Enqueue(new Event1("abc"));
            reactor.Update(0.1f);
            Assert.Equal("abc", reactor.Data);
        }

        class Event2 : IReactorEvent
        {
            public IReactorReference Source => null;
        }

        class Reactor2 : Reactor
        {
            public bool ReceivedEvent2 { get; private set; }

            class Coroutine1 : Coroutine
            {
                Reactor reactor;
                public Coroutine1(Reactor reactor)
                {
                    this.reactor = reactor;
                }

                protected override IEnumerator<IWaitObject> Execute()
                {
                    yield return null;
                    reactor.Enqueue(new Event2());
                }
            }

            protected override void OnEvent(IReactorEvent ev)
            {
                switch (ev)
                {
                    case Event1 ev1:
                        Execute(new Coroutine1(this));
                        break;
                    case Event2 ev2:
                        ReceivedEvent2 = true;
                        break;
                    default:
                        throw new Exception("Invalid event");
                }
            }
        }

        [Fact]
        public void ProcessCoroutine()
        {
            var reactor = new Reactor2();

            reactor.Update(0.1f);
            reactor.Enqueue(new Event1("abc"));
            Assert.False(reactor.ReceivedEvent2);
            reactor.Update(0.1f);
            Assert.False(reactor.ReceivedEvent2);
            // We yield null so we reply next frame
            reactor.Update(0.1f);
            Assert.True(reactor.ReceivedEvent2);
        }
    }
}
