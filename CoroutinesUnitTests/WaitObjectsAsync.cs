using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Coroutines.Tests
{
    public class WaitObjectsAsync
    {
        IEnumerator<IWaitObject> Coroutine1()
        {
            yield return null;
            using (var file = File.OpenText("testpath.txt"))
            {
                var waitObject = new AsyncWait<string>(file.ReadLineAsync());
                yield return waitObject;
                
            }
        }

        [Fact]
        public void Test1()
        {
            
        }
    }
}
