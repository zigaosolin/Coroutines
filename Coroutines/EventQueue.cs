using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Coroutines
{
    [JsonObject(MemberSerialization.Fields)]
    public class EventQueue : IEventPusher
    {
        object syncRoot = new object();
        Queue<IEvent> updateQueue = new Queue<IEvent>();
        Queue<IEvent> nextFrameQueue = new Queue<IEvent>();
        
        public EventQueue()
        {
        }


        public void Enqueue(IEvent ev)
        {
            lock(syncRoot)
            {
                updateQueue.Enqueue(ev);
            }
        }

        public void EnqueueNextFrame(IEvent ev)
        {
            lock (syncRoot)
            {
                nextFrameQueue.Enqueue(ev);
            }
        }

        public bool TryDequeue(out IEvent ev)
        {
            lock(syncRoot)
            {
                if(updateQueue.Count > 0)
                {
                    ev = updateQueue.Dequeue();
                    return true;
                }

                ev = null;
                return false;
            }
        }

        public int Count
        {
            get {
                lock (syncRoot)
                {
                    return updateQueue.Count + nextFrameQueue.Count;
                }
            }
        }

        public int CountCurrentFrame
        {
            get
            {
                lock (syncRoot)
                {
                    return updateQueue.Count;
                }
            }
        }

        public void NewFrame()
        {
            lock(syncRoot)
            {
                // We transfer all events from next frame queue to update queue
                while(nextFrameQueue.Count > 0)
                {
                    updateQueue.Enqueue(nextFrameQueue.Dequeue());
                }
            }
        }
    }
}
