using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines.Implementation
{
    internal class WaitForSecondsCoroutine : Coroutine
    {
        float waitTime;

        public WaitForSecondsCoroutine(float seconds)
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
