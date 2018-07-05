using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines.Implementation
{
    internal class SmartTimerTrigger
    {
        // The idea is that the timer has a queue of all timeouts. When delta time
        // update is called, the operation should be O(1), so we don't update all
        // states waiting to be trigerred. Enqueuing is O(log n)

        // This class will allow WaitForSeconds to be replaced with trigerred wait
        // for seconds. This would make timeout coroutines almost free in this
        // system

    }
}
