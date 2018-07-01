using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Coroutines
{
    public class ThreadWait : IWaitObject
    {
        Thread thread;

        public ThreadWait(Thread thread)
        {
            this.thread = thread;
            if(thread.ThreadState == ThreadState.Unstarted)
            {
                thread.Start();
            }
        }

        public bool IsComplete
        {
            get
            {
                return thread.ThreadState == ThreadState.Stopped;
            }
        }
            
    }
}
