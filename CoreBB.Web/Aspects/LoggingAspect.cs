using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using PostSharp.Aspects;

namespace CoreBB.Web.Aspects
{
    [Serializable]
    public class LoggingAspect : OnMethodBoundaryAspect
    {
        private Stopwatch stopWatch;

        public override void OnEntry(MethodExecutionArgs args)
        {
            using (StreamWriter sw = File.AppendText(@"C:\someStupidLog.txt"))
            {
                stopWatch = new Stopwatch();
                stopWatch.Start();
                string onEntry = $"Controller {args.Method} called at {DateTime.Now}";
                sw.WriteLine(onEntry);
            }
        }

        public override void OnExit(MethodExecutionArgs args)
        {
            stopWatch.Stop();
            using (StreamWriter sw = File.AppendText(@"C:\someStupidLog.txt"))
            {
                string onExit = $"Controller {args.Method} finished at {DateTime.Now}";
                string timeSpan = $"Controller {args.Method} spend {stopWatch.Elapsed} for execution";
                sw.WriteLine(onExit);
                sw.WriteLine(timeSpan);
            }
        }
    }
}
