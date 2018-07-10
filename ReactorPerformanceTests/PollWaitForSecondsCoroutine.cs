using Coroutines;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors.Performance
{
    public class PollWaitForSecondsCoroutine : Coroutine
    {
        float time;
        public PollWaitForSecondsCoroutine(float time)
        {
            this.time = time;
        }

        protected override IEnumerator<IWaitObject> Execute()
        {
            float current = 0;
            while(current < time)
            {
                yield return null;
                current += ExecutionState.DeltaTime;
            }
        }
    }
}
