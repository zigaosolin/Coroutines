using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines
{
    public class ExecutionState
    {
        public float DeltaTime { get; }
        public long FrameIndex { get; }

        public ExecutionState(float deltaTime, long frameIndex)
        {
            DeltaTime = deltaTime;
            frameIndex = frameIndex;
        }
    }
}
