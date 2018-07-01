using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines.Implementation
{
    public class SimpleCoroutine : Coroutine
    {
        IEnumerator<IWaitObject> executor;

        public SimpleCoroutine(IEnumerator<IWaitObject> executor)
        {
            this.executor = executor;
        }

        protected internal override IEnumerator<IWaitObject> Execute()
        {
            return executor;
        }
    }
}
