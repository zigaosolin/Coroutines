using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Reactors
{
    public class ReactorIsland
    {
        volatile bool isRunning = false;
        object syncRoot = new object();
        List<Reactor> reactors = new List<Reactor>();

        TimeSpan desiredDeltaTime = new TimeSpan(0);
        bool stopRequested = false;

        public Thread Thread { get; private set; }

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

        public bool RemoveReactor(Reactor reactor)
        {
            lock(syncRoot)
            {
                return reactors.Remove(reactor);
            }
        }

        public async Task RunAsTaskWithDelays(float desiredDeltaTime)
        {
            AssertNotRunning();
            isRunning = true;
            this.desiredDeltaTime = TimeSpan.FromSeconds(desiredDeltaTime);

            TimeSpan deltaTime = new TimeSpan(0);
            DateTime start = DateTime.UtcNow;
            DateTime end;

            while(!stopRequested)
            {
                start = DateTime.UtcNow;

                lock(syncRoot)
                {
                    float deltaTimeFloat = (float)deltaTime.TotalSeconds;
                    foreach (var reactor in reactors)
                    {
                        reactor.Update(deltaTimeFloat);
                    }
                }

                end = DateTime.UtcNow;
                deltaTime = end - start;

                // We delay and recalculate delta time
                if(this.desiredDeltaTime > deltaTime)
                {
                    await Task.Delay(this.desiredDeltaTime - deltaTime);
                    end = DateTime.UtcNow;
                    deltaTime = end - start;
                }
            }

            isRunning = false;
        }

        public Thread RunAsThread(float desiredDeltaTime)
        {
            AssertNotRunning();
            isRunning = true;
            this.desiredDeltaTime = TimeSpan.FromSeconds(desiredDeltaTime);

            Thread = new Thread(ThreadStart);
            Thread.Start();
            return Thread;
        }

        public void RequestStop()
        {
            if(!isRunning)
            {
                throw new ReactorException("Cannot stop reactor island, it is not running");
            }

            stopRequested = true;
        }

        public bool IsRunning
        {
            get
            {
                return isRunning;
            }
        }

        void ThreadStart()
        { 
            TimeSpan deltaTime = TimeSpan.FromSeconds(0);
            DateTime start = DateTime.UtcNow;
            DateTime end;

            while (!stopRequested)
            {
                start = DateTime.UtcNow;

                lock (syncRoot)
                {
                    float deltaTimeFloat = (float)deltaTime.TotalSeconds;
                    foreach (var reactor in reactors)
                    {
                        reactor.Update(deltaTimeFloat);
                    }
                }

                end = DateTime.UtcNow;
                deltaTime = (end - start);

                // We delay and recalculate delta time
                if (desiredDeltaTime > deltaTime)
                {
                    Thread.Sleep(desiredDeltaTime - deltaTime);
                    end = DateTime.UtcNow;
                    deltaTime = end - start;
                }
            }

            isRunning = false;
        }

        void AssertNotRunning()
        {
            if(isRunning)
            {
                throw new ReactorException("Multiple Run* commands were issued on reactor island");
            }
        }
    }
}
