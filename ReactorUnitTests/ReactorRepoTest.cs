using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Reactors.Tests
{
    public class ReactorRepoTest
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
            public Reactor1(string uniqueName)
                : base(uniqueName)
            {
            }

            protected override void OnEvent(IReactorEvent ev)
            {
                switch (ev)
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
        public async Task TwoReactors_TaskWithDelays()
        {
            var repo = ReactorRepository.CreateLocal(1, 0.01f, useTasks: true);
            var reactors = new ReactorBase[] { new Reactor1("1"),
                                               new Reactor1("2") };
            repo.Add(reactors[0]);
            repo.Add(reactors[1]);

            for (int i = 0; i < 20; i++)
            {
                reactors[i % 2].Enqueue(null, new Event1(i.ToString()));
                await Task.Delay(10);
            }

            await Task.Delay(30);

            Assert.Equal("18", ((Reactor1)reactors[0]).State.Data);
            Assert.Equal("19", ((Reactor1)reactors[1]).State.Data);
        }

        [Fact]
        public async Task TwoReactors_Threads()
        {
            var repo = ReactorRepository.CreateLocal(1, 0.01f, useTasks: false);
            var reactors = new ReactorBase[] { new Reactor1("1"),
                                           new Reactor1("2") };
            repo.Add(reactors[0]);
            repo.Add(reactors[1]);

            for (int i = 0; i < 20; i++)
            {
                reactors[i % 2].Enqueue(null, new Event1(i.ToString()));
                await Task.Delay(10);
            }

            await Task.Delay(30);

            Assert.Equal("18", ((Reactor1)reactors[0]).State.Data);
            Assert.Equal("19", ((Reactor1)reactors[1]).State.Data);
        }

    }
}
