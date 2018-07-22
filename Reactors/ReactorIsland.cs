using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Reactors
{
    public class ReactorIsland
    {
        object syncRoot = new object();
        List<ReactorBase> reactors = new List<ReactorBase>();     
        TimeSpan desiredDeltaTime = new TimeSpan(0);
        bool stopRequested = false;
        public bool IsRunning { get; private set; } = false;

        public Thread Thread { get; private set; }

        internal ReactorIsland(params ReactorBase[] reactors)
        {
            foreach(var reactor in reactors)
            {
                AddReactor(reactor);
            }
        }

        public IReadOnlyList<ReactorBase> Reactors
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

        public void AddReactor(ReactorBase reactor)
        {
            lock(syncRoot)
            {
                reactors.Add(reactor);
            }
        }

        public bool RemoveReactor(ReactorBase reactor)
        {
            lock(syncRoot)
            {
                return reactors.Remove(reactor);
            }
        }

        public async Task RunAsTaskWithDelays(float desiredDeltaTime)
        {
            AssertNotRunning();
            IsRunning = true;
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
                    for(int i = 0; i < reactors.Count; i++)
                    {
                        reactors[i].Update(deltaTimeFloat);
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

            IsRunning = false;
        }

        public Thread RunAsThread(float desiredDeltaTime)
        {
            AssertNotRunning();
            IsRunning = true;
            this.desiredDeltaTime = TimeSpan.FromSeconds(desiredDeltaTime);

            Thread = new Thread(ThreadStart);
            Thread.Start();
            return Thread;
        }

        public void RequestStop()
        {
            if(!IsRunning)
            {
                throw new ReactorException("Cannot stop reactor island, it is not running");
            }

            stopRequested = true;
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
                    for (int i = 0; i < reactors.Count; i++)
                    {
                        reactors[i].Update(deltaTimeFloat);
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

            IsRunning = false;
        }

        void AssertNotRunning()
        {
            if(IsRunning)
            {
                throw new ReactorException("Multiple Run* commands were issued on reactor island");
            }
        }
    }
}
