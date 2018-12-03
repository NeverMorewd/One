using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utilities
{
    public  class Tool:IDisposable
    {
        public delegate void TestEvent(string s);
        public Tool()
        {

        }

        public  event  TestEvent ToolTestEvent;

        public void OnTest(string s)
        {
            if (this.ToolTestEvent!=null)
            {
                this.ToolTestEvent(s);
            }
            
        }

        public void Dispose()
        {
           
        }
    }
}
