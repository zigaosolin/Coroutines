using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines
{

    public interface ICoroutineEvent
    {
    }

    public interface IEventQueue
    {
        void Enqueue(object ev, bool nextFrame = false);
    }
}
