using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaselineSystem
{
    public class LogEventArgs
    {
        public DateTime time;
        public string log;
        public LogEventArgs(string log)
        {
            this.log = log;
            time = System.DateTime.Now;
        }
    }
}
