using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines
{
    public class ExecutionState
    {
        public float DeltaTime { get; internal set; }
        public long FrameIndex { get; internal set; }
    }
}
