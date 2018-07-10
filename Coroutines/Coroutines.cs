using Coroutines.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        public static IWaitObject WaitForSeconds(float time)
        {
            return new WaitForSeconds(time);
        }

        public static Coroutine NextFrame()
        {
            return null;
        }

        public static Coroutine<T> WaitForAsync<T>(Task<T> task)
        {
            return new AsyncWait<T>(task);
        }

        public static Coroutine WaitForAsync(Task task)
        {
            return new AsyncWait(task);
        }

        public static IWaitObject WaitForThread(Thread thread)
        {
            return new ThreadWait(thread);
        }

        public static IWaitObject WaitForAny(params IWaitObject[] waitObjects)
        {
            return new WaitForCountCoroutine(waitObjects.ToList(), 1, false);
        }

        public static IWaitObject WaitForAnyCancelOthers(params IWaitObject[] waitObjects)
        {
            return new WaitForCountCoroutine(waitObjects.ToList(), 1, true);
        }

        public static IWaitObject WaitForAll(params IWaitObject[] waitObjects)
        {
            return new WaitForCountCoroutine(waitObjects.ToList(), waitObjects.Length, false);
        }

        public static IWaitObject WaitForCount(int count, params IWaitObject[] waitObjects)
        {
            return new WaitForCountCoroutine(waitObjects.ToList(), count, false);
        }

        public static IWaitObject WaitForCountCancelOthers(int count, params IWaitObject[] waitObjects)
        {
            return new WaitForCountCoroutine(waitObjects.ToList(), count, true);
        }
    }
}
