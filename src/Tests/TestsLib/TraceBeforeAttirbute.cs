//using System;
//using System.Diagnostics;
//using System.Reflection;
//using Xunit.Sdk;
//
//namespace TestsLib{
//    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
//    public class TraceBeforeAttirbute : BeforeAfterTestAttribute{
//        public override void Before(MethodInfo methodUnderTest){
//            BaseTest.TraceSource.TraceEvent(TraceEventType.Information, 0,
//                $"------------------------{methodUnderTest.Name}----------------------");
//        }
//
//        public override void After(MethodInfo methodUnderTest){
//        }
//    }
//}