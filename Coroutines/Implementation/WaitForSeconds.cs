using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines.Implementation
{
    internal class WaitForSecondsCoroutine : Coroutine
    {
        public float WaitTime { get; }

        public WaitForSecondsCoroutine(float seconds)
        {
            WaitTime = seconds;
        }

        protected internal override IEnumerator<IWaitObject> Execute()
        {
            // Scheduler usually implements special override for this coroutine
            // so there is no polling, i.e. this is never executed

            float time = 0.0f;
            while (time < WaitTime)
            {
                yield return null;
                time += ExecutionState.DeltaTime;
            }
        }
    }
}
