using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines.Implementation
{
    internal class WaitForSeconds : IWaitObject
    {
        public float WaitTime { get; }

        public bool IsComplete => false;

        public Exception Exception => null;

        public WaitForSeconds(float seconds)
        {
            WaitTime = seconds;
        }
    }
}
