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

        class Reactor1State
        {
            public string Data { get; set; }
        }

        class Reactor1 : Reactor<Reactor1State>
        {
            public Reactor1()
                : base("Reactor1")
            {
            }

            protected override void OnEvent(IReactorEvent ev)
            {
                switch(ev)
                {
                    case Event1 ev1:
                        State.Data = ev1.Data;
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
            Assert.Null(reactor.State.Data);
            reactor.Enqueue(null, new Event1("abc"));
            reactor.Update(0.1f);
            Assert.Equal("abc", reactor.State.Data);
        }

        class Event2 : IReactorEvent
        {
            public IReactorReference Source => null;
        }

        class Reactor2State
        {
            public bool ReceivedEvent2 { get; set; }
        }

        class Reactor2 : Reactor<Reactor2State>
        {
            public Reactor2()
                : base("Reactor2")
            {
            }

            class Coroutine1 : Coroutine
            {
                ReactorBase reactor;
                public Coroutine1(ReactorBase reactor)
                {
                    this.reactor = reactor;
                }

                protected override IEnumerator<IWaitObject> Execute()
                {
                    yield return null;
                    reactor.Enqueue(null, new Event2());
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
                        State.ReceivedEvent2 = true;
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
            reactor.Enqueue(null, new Event1("abc"));
            Assert.False(reactor.State.ReceivedEvent2);
            reactor.Update(0.1f);
            Assert.False(reactor.State.ReceivedEvent2);
            // We yield null so we reply next frame
            reactor.Update(0.1f);
            Assert.True(reactor.State.ReceivedEvent2);
        }
    }
}
