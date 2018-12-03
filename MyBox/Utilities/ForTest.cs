using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace Utilities
{
    public class ForTest
    {

        public ForTest()
        {
        }

        public void start()
        {
            Test1 t1 = new Test1();
            t1.start();
        }

        internal class Test1
        {
            //private Tool tool;
            private WebBrowser webBrowser1;
            private System.Timers.Timer keepAliveServiceTimer = null;
            private Thread t;
            public Test1()
            {
                keepAliveServiceTimer = new System.Timers.Timer(50);
                keepAliveServiceTimer.Elapsed += KeepAliveServiceTimer_Elapsed;
            }

            private void KeepAliveServiceTimer_Elapsed(object sender, ElapsedEventArgs e)
            {
                keepAliveServiceTimer.Stop();
                if (t.IsAlive)
                {
                    t.Abort();
                    t = null;
                }
                t = new Thread(Handle);
                t.SetApartmentState(ApartmentState.STA);
                t.IsBackground = true;
                t.Start();
                t.Join();
                keepAliveServiceTimer.Start();
            }

            public void start()
            {
                keepAliveServiceTimer.Start();
            }

            private void Handle()
            {
               // tool = new Tool();
                webBrowser1 = new WebBrowser();
                //tool.ToolTestEvent += Tool_ToolTestEvent;
                webBrowser1.DocumentCompleted += WebBrowser1_DocumentCompleted;
                webBrowser1.Navigate("http://www.baidu.com/");
               // tool.OnTest(DateTime.Now.ToString());
               // tool.Dispose();
                webBrowser1.Dispose();

            }

            private void WebBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
            {
                //LogHelper.OutputLog("webBrowser1: "+DateTime.Now);
                //webBrowser1.DocumentCompleted -= WebBrowser1_DocumentCompleted;               
            }

            private void Tool_ToolTestEvent(string s)
            {
                //LogHelper.OutputLog(s);
                //tool.ToolTestEvent -= Tool_ToolTestEvent;
            }
        }


    }
}
