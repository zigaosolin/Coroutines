using System;
using System.Collections.Generic;
using System.Text;

namespace Coroutines
{
    public class CoroutineException : Exception
    {
        public CoroutineException(string message) : base(message)
        {
        }
    }
}
