using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpSocket
{
    public class Logger
    {
        public static void write(object s)
        {
            Console.WriteLine("[SharpSocket] [" + DateTime.Now.ToString("hh:mm:ss tt") + "]: " + s);
        }
    }
}
