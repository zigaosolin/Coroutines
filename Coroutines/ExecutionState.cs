using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines
{
    public interface IExecutionState
    {
        float DeltaTime { get; }
        long FrameIndex { get; }
    }
}
