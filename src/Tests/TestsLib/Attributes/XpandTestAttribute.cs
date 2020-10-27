using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Fasterflect;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using Polly.Timeout;
using Xpand.Extensions.TaskExtensions;

namespace Xpand.TestsLib.Attributes{
    [AttributeUsage(AttributeTargets.Method|AttributeTargets.Assembly, Inherited = false)]
    public class XpandTestAttribute : Attribute,IRepeatTest{
        private readonly int _tryCount;
        private readonly int _timeout;

        public XpandTestAttribute(int timeout = 60000, int tryCount = 2){
            _timeout = timeout;
            _tryCount = tryCount;
        }

        public string IgnoredXAFVersions{ get; set; }
        public TestCommand Wrap(TestCommand command){
            IgnoredXAFVersions ??= command.Test.Method.MethodInfo.DeclaringType?.Assembly.Attribute<XpandTestAttribute>()?.IgnoredXAFVersions;
            return new RetryCommand(command, _tryCount, _timeout, IgnoredXAFVersions);
        }

        public class RetryCommand : DelegatingTestCommand{
            private readonly int _tryCount;
            private readonly int _timeout;
            private readonly string _ignoredXAFVersions;

            public RetryCommand(TestCommand innerCommand, int tryCount, int timeout, string ignoredXAFVersions)
                : base(innerCommand){
                _tryCount = tryCount;
                _timeout = timeout;
                _ignoredXAFVersions = ignoredXAFVersions;
            }

            public override TestResult Execute(TestExecutionContext context){
                var count = _tryCount;
                var version = Version.Parse(XafAssemblyInfo.Version);
                if ($"{version.Major}.{version.Minor}" == _ignoredXAFVersions){
                    // context.CurrentTest.MakeInvalid(_ignoredXAFVersions);
                    context.CurrentTest.RunState=RunState.Skipped;
                    return context.CurrentResult;
                }
                while (count-- > 0){
                    try{
                        // ManualResetEvent resetEvent = new ManualResetEvent(false);
                        TestResult ExecuteTest() => context.CurrentResult = innerCommand.Execute(context);
                        if (Debugger.IsAttached) {
                            ExecuteTest();
                        }
                        else {
                            Polly.Policy.Timeout(TimeSpan.FromMilliseconds(_timeout), TimeoutStrategy.Pessimistic)
                                .Execute(() => {
                                    Task.Factory.StartTask(ExecuteTest,
                                        thread => thread.SetApartmentState(GetApartmentState(context))).Wait();
                                    // return;
                                    // if (context.CurrentTest.Arguments.Any(o => o == (object) Platform.Web)){
                                    //     ThreadPool.QueueUserWorkItem(state => {
                                    //         var tuple = ((TestExecutionContext executionContext, TestCommand command)) state;
                                    //         tuple.executionContext.CurrentResult = tuple.command.Execute(tuple.executionContext);
                                    //         resetEvent.Set();
                                    //     }, (context, innerCommand));
                                    //     resetEvent.WaitOne();    
                                    // }
                                    // else{
                                    //     Task.Factory.StartTask(Execute, thread => thread.SetApartmentState(GetApartmentState(context))).Wait();   
                                    // }
                                });
                        };
                    }
                    catch (Exception ex){
                        context.CurrentResult ??= context.CurrentTest.MakeTestResult();
                        var message = $"Retry {context.CurrentRepeatCount+1} of {_tryCount}";
                        TestContext.Out.WriteLine(message);
                        context.CurrentResult.RecordException(new Exception(message,ex));
                    }

                    if (count > 0){
                        if (context.CurrentResult.ResultState!=ResultState.Success){
                            context.CurrentRepeatCount++;
                        }
                        else{
                            break;
                        }
                    }
                }

                return context.CurrentResult;
            }
            
            private static ApartmentState GetApartmentState(TestExecutionContext context){
                var apartmentAttribute = context.CurrentTest.Method.MethodInfo.Attribute<ApartmentAttribute>();
                return (ApartmentState?) apartmentAttribute?.Properties[PropertyNames.ApartmentState].OfType<object>().First() ?? Thread.CurrentThread.GetApartmentState();
            }
        }
    }
}