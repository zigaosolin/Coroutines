using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Coroutines.Tests
{
    public class WaitObjectsAsyncTest
    {
        IEnumerator<IWaitObject> Coroutine1()
        {
            yield return null;
            using (var file = File.OpenText("testpath.txt"))
            {
                var waitObject = new AsyncWait<string>(file.ReadLineAsync());
                yield return waitObject;
                string line = waitObject.Result;
                
            }
        }

        [Fact]
        public void Test1()
        {
            Coroutines.FromEnumerator(Coroutine1());
        }
    }
}
