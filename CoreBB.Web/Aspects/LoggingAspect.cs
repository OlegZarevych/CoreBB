using System;
using PostSharp.Aspects;

namespace CoreBB.Web.Aspects
{
    [Serializable]
    public class LoggingAspect : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs args)
        {

        }

        public override void OnExit(MethodExecutionArgs args)
        {

        }
    }
}
