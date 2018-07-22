using System;
using System.Collections.Generic;
using System.Text;

namespace Chat.Server
{
    public class ChatException : Exception
    {
        public ChatException(string message) : base(message)
        {
        }
    }
}
