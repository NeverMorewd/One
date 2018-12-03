using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Utilities
{
    public class LogHelper
    {
        static object locker = new object();
        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="ex"></param>
        public static void OutputErrorLog(Exception ex)
        {

            TextWriter writer = null;
            try
            {
                Monitor.Enter(locker);
                string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string path = @"C:\Testlog";
                if (Directory.Exists(path) == false)
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = Path.Combine(path, DateTime.Now.Date.ToString("yyyy-MM-dd") + ".error.log");

                StringBuilder message = new StringBuilder();

                message.Append("Time:");
                message.Append(dateTime);
                message.Append(Environment.NewLine);
                message.Append("Message:");
                message.Append(ex.Message);
                message.Append(Environment.NewLine);
                message.Append("StackTrace : ");
                message.Append(ex.StackTrace);
                message.Append(Environment.NewLine);

                FileInfo fileInfo = new FileInfo(fileName);

                if (File.Exists(fileName) == true)
                {
                    writer = fileInfo.AppendText();
                }
                else
                {
                    writer = fileInfo.CreateText();
                }

                writer.WriteLine();

                writer.WriteLine(message.ToString());

                writer.Flush();
            }
            finally
            {
                writer.Close();

                writer = null;

                Monitor.Exit(locker);
            }
        }

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="ex"></param>
        public static void OutputLog(string message)
        {

            TextWriter writer = null;
            try
            {
                Monitor.Enter(locker);
                string path = @"C:\Testlog";
                if (Directory.Exists(path) == false)
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = Path.Combine(path, DateTime.Now.Date.ToString("yyyy-MM-dd") + ".log");

                FileInfo fileInfo = new FileInfo(fileName);

                if (File.Exists(fileName) == true)
                {
                    writer = fileInfo.AppendText();
                }
                else
                {
                    writer = fileInfo.CreateText();
                }

                writer.WriteLine();

                writer.WriteLine(DateTime.Now.ToString());

                writer.WriteLine(message);

                writer.WriteLine("==============================================================================");

                writer.Flush();
            }
            finally
            {
                writer.Close();

                writer = null;

                Monitor.Exit(locker);
            }
        }

    }
}
