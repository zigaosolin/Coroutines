using Coroutines;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors.Performance
{
    public class WaitForSecondsCoroutine : Coroutine
    {
        float time;
        public WaitForSecondsCoroutine(float time)
        {
            this.time = time;
        }

        protected override IEnumerator<IWaitObject> Execute()
        {
            yield return WaitForSeconds(time);
        }
    }
}
