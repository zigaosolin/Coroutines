using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines
{
    internal class ReturnValue : IWaitObject
    {
        public object Result { get; }

        public bool IsComplete => true;
        public Exception Exception => null;

        internal ReturnValue(object result)
        {
            Result = result;
        }

    }
}
