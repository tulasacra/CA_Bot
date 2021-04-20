using System;
using System.Collections.Generic;
using System.Text;

namespace CA_Bot
{
    static class Log
    {
        public static void WriteLine(string message)
        {
            Console.WriteLine($"{DateTime.Now} {message}");
        }
    }
}
