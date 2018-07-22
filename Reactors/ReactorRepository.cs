using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Reactors
{
    internal class LocalReactorReference : IReactorReference
    {
        private ReactorBase reactor;

        public LocalReactorReference(string name, ReactorBase reactor)
        {
            Reference = name;
            this.reactor = reactor;
        }

        public string Reference { get; }

        public void Send(IReactorReference source, IReactorEvent ev, long eventID, long replyID)
        {
            reactor.Enqueue(source, ev, eventID, replyID);
        }
    }

    public class ReactorRepository
    {
        public static ReactorRepository Global { get; private set; }

        ConcurrentDictionary<string, Tuple<ReactorIsland,ReactorBase>> reactors
            = new ConcurrentDictionary<string, Tuple<ReactorIsland,ReactorBase>>();

        volatile int currentIsland = 0;
        volatile ReactorIsland[] islands;

        public static ReactorRepository CreateGlobal(int numberOfIslands, float desiredUpdateTime = 0.04f, bool useTasks = false)
        {
            if(Global != null)
            {
                throw new ReactorException("Global reactor repository already exists");
            }

            Global = new ReactorRepository(numberOfIslands, desiredUpdateTime, useTasks);
            return Global;
        }

        public static ReactorRepository CreateLocal(int numberOfIslands, float desiredUpdateTime = 0.04f, bool useTasks = false)
        {
            return new ReactorRepository(numberOfIslands, desiredUpdateTime, useTasks);
        }

        ReactorRepository(int numberOfIslands, float desiredUpdateTime = 0.04f, bool useTasks = false)
        {
            islands = new ReactorIsland[numberOfIslands];
            for(int i = 0; i < numberOfIslands; i++)
            {
                islands[i] = new ReactorIsland();
                if(useTasks)
                {
                    Task unawaited = islands[i].RunAsTaskWithDelays(desiredUpdateTime);
                }
                else
                {
                    islands[i].RunAsThread(desiredUpdateTime);
                }
            }
        }

        public ReactorIsland Add(ReactorBase reactor, int reactorIslandIndex = -1)
        {
            string referenceName = reactor.GetType().FullName + ":" + reactor.Name;

            int index = reactorIslandIndex;
            if (index < 0)
            {
                reactorIslandIndex = Interlocked.Increment(ref currentIsland);
            }
            var island = islands[index % islands.Length];

            if (!reactors.TryAdd(referenceName, new Tuple<ReactorIsland, ReactorBase>(island, reactor)))
            {
                throw new ReactorException($"Failed to add reactor {referenceName}, it already exists");
            }

            reactor.AttachedToRepository(Resolve(reactor.GetType(), reactor.Name), this);
            island.AddReactor(reactor);

            return island;
        }

        public ReactorIsland GetIsland(IReactorReference reference)
        {
            if(reference is LocalReactorReference lref)
            {
                if (!reactors.TryGetValue(lref.Reference, out Tuple<ReactorIsland, ReactorBase> result))
                    return null;

                return result.Item1;
            }
            throw new ReactorException("Only local references supported at the moment");
        }

        public void Remove(ReactorBase reactor)
        {
            string referenceName = reactor.GetType().FullName + ":" + reactor.Name;
            if(reactors.TryRemove(referenceName, out Tuple<ReactorIsland, ReactorBase> removed))
            {
                reactor.AttachedToRepository(null, null);
                removed.Item1.RemoveReactor(removed.Item2);
            }
        }

        public IReactorReference Resolve<TState>(Reactor<TState> reactor)
            where TState : class, new()
        {
            return Resolve(reactor.GetType(), reactor.Name);
        }

        public IReactorReference Resolve<T>(string name)
        {
            return Resolve(typeof(T), name);
        }

        public IReactorReference Resolve(Type type, string name)
        {
            string referenceName = type.FullName + ":" + name;
            if(reactors.TryGetValue(referenceName, out Tuple<ReactorIsland, ReactorBase> tuple))
            {
                return new LocalReactorReference(referenceName, tuple.Item2);
            }

            throw new ReactorException($"Failed to find actor {referenceName}");
        }

    }
}
