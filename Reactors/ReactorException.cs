using System;
using System.Collections.Generic;
using System.Text;

namespace Reactors
{
    public class ReactorException : Exception
    {
        public ReactorException()
        {
        }

        public ReactorException(string message) : base(message)
        {
        }
    }
}
