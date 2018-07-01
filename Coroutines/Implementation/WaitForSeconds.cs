using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines.Implementation
{
    internal class WaitForSeconds : Coroutine
    {
        float waitTime;

        public WaitForSeconds(float seconds)
        {
            waitTime = seconds;
        }

        protected internal override IEnumerator<IWaitObject> Execute()
        {
            float time = 0.0f;
            while (time < waitTime)
            {
                yield return null;
                time += ExecutionState.DeltaTime;
            }
        }
    }
}
