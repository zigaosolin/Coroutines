using System;
using System.Collections.Generic;
using System.Text;
using Coroutines;

namespace Reactors.Performance
{
    public class SimpleCoroutine : Coroutine
    {
        int loops;

        public SimpleCoroutine(int loops)
        {
            this.loops = loops;
        }

        protected override IEnumerator<IWaitObject> Execute()
        {
            for(int i = 0; i < loops; i++)
            {
                yield return null;
            }
        }
    }
}
