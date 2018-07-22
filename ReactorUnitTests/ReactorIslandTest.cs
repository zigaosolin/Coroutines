using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Reactors.Tests
{
    public class ReactorIslandTest
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
            var island = new ReactorIsland(
                new Reactor1("1"), new Reactor1("2"));

            Task islandTask = Task.Run(async () => await island.RunAsTaskWithDelays(0.02f));

            for(int i = 0; i < 2; i++)
            {
                island.Reactors[i].Enqueue(null, new Event1(i.ToString()));
                await Task.Delay(100);
            }

            Assert.True(island.IsRunning);
            island.RequestStop();
            await islandTask;

            Assert.Equal("0", ((Reactor1)island.Reactors[0]).State.Data);
            Assert.Equal("1", ((Reactor1)island.Reactors[1]).State.Data);
        }

        [Fact]
        public async Task TwoReactors_Thread()
        {
            var island = new ReactorIsland(
                new Reactor1("1"), new Reactor1("2"));

            var thread = island.RunAsThread(0.02f);

            for (int i = 0; i < 2; i++)
            {
                island.Reactors[i].Enqueue(null, new Event1(i.ToString()));
                await Task.Delay(100);
            }

            Assert.True(island.IsRunning);
            island.RequestStop();
            thread.Join();

            Assert.Equal("0", ((Reactor1)island.Reactors[0]).State.Data);
            Assert.Equal("1", ((Reactor1)island.Reactors[1]).State.Data);
        }

    }
}
