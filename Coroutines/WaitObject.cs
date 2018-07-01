﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Coroutines
{
    public interface IWaitObject
    {
        bool IsComplete { get; }
        void OnCompleted(Action continuation);
    }
}
