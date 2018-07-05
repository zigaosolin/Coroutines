using Coroutines.Implementation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines
{
    // We wrap coroutines to make them more abstract. This means we can later
    // optimize/reimplement them differently.

    public static class Coroutines
    {
        public static Coroutine FromEnumerator(IEnumerator<IWaitObject> executor)
        {
            return new SimpleCoroutine(executor);
        }

        public static Coroutine WaitForSeconds(float time)
        {
            return new WaitForSecondsCoroutine(time);
        }

        public static Coroutine NextFrame()
        {
            return null;
        }
    }
}
