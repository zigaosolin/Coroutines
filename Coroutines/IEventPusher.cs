using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines
{
    public interface IEvent
    {
    }

    public interface IEventPusher
    {
        void Enqueue(IEvent ev);
        void EnqueueNextFrame(IEvent ev);
    }
}
