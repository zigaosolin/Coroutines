using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Reactors
{
    public class ReactorIsland
    {
        object syncRoot = new object();
        List<Reactor> reactors = new List<Reactor>();

        public ReactorIsland(params Reactor[] reactors)
        {
            this.reactors = reactors.ToList();
        }

        public IReadOnlyList<Reactor> Reactors
        {
            get
            {
                lock (syncRoot)
                {
                    // We need copy to be thread safe
                    return reactors.ToList();
                }
            }
        }

        public void AddReactor(Reactor reactor)
        {
            lock(syncRoot)
            {
                reactors.Add(reactor);
            }
        }

        public void RemoveReactor(Reactor reactor)
        {
            lock(syncRoot)
            {
                reactors.Remove(reactor);
            }
        }

        public async Task RunWithDelays(float desiredDeltaTime)
        {
            float deltaTime = 0;
            DateTime start = DateTime.UtcNow;
            DateTime end;

            while(true)
            {
                start = DateTime.UtcNow;

                lock(syncRoot)
                {
                    foreach(var reactor in reactors)
                    {
                        reactor.Update(deltaTime);
                    }
                }

                end = DateTime.UtcNow;
                deltaTime = (float)(end - start).TotalSeconds;

                // We delay and recalculate delta time
                if(desiredDeltaTime > deltaTime)
                {
                    await Task.Delay((int)((desiredDeltaTime - deltaTime) * 1000));
                    end = DateTime.UtcNow;
                    deltaTime = (float)(end - start).TotalSeconds;
                }
            }
        }
    }
}
