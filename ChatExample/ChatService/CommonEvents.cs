using Reactors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chat
{
    public sealed class OK : IReactorEvent
    {
    }

    public sealed class Error : IReactorEvent
    {
        public string ErrorMessage { get; }

        public Error(string errorMessage = "")
        {
            ErrorMessage = errorMessage;
        }
    }
}
