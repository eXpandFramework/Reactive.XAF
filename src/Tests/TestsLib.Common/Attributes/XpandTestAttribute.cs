using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

namespace Xpand.TestsLib.Common.Attributes{
    [AttributeUsage(AttributeTargets.Method|AttributeTargets.Assembly, Inherited = false)]
    public class XpandTestAttribute : Attribute,IRepeatTest{
        private readonly int _tryCount;
        private readonly ApartmentState _state;
        private readonly int _timeout;

        public XpandTestAttribute(int timeout = 60000, int tryCount = 3,ApartmentState state=ApartmentState.STA){
            _timeout = timeout;
            _tryCount = tryCount;
            _state = state;
        }

        public string IgnoredXAFMinorVersions{ get; set; }
        public TestCommand Wrap(TestCommand command){
            IgnoredXAFMinorVersions ??= command.Test.Method?.MethodInfo.DeclaringType?.Assembly.Attribute<XpandTestAttribute>()?.IgnoredXAFMinorVersions;
            return new RetryCommand(command, _tryCount, _timeout, IgnoredXAFMinorVersions,_state);
        }

        public class RetryCommand : DelegatingTestCommand{
            private readonly int _tryCount;
            private readonly int _timeout;
            private readonly string _ignoredXAFMinorVersions;
            private readonly ApartmentState _apartmentState;

            public RetryCommand(TestCommand innerCommand, int tryCount, int timeout, string ignoredXAFMinorVersions,
                ApartmentState apartmentState)
                : base(innerCommand){
                _tryCount = tryCount;
                _timeout = timeout;
                _ignoredXAFMinorVersions = ignoredXAFMinorVersions;
                _apartmentState = apartmentState;
            }

            [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
            public override TestResult Execute(TestExecutionContext context){
                var count = _tryCount;
                var version = Version.Parse(XafAssemblyInfo.Version);
                if ($"{version.Major}.{version.Minor}" == _ignoredXAFMinorVersions){
                    context.CurrentTest.RunState=RunState.Skipped;
                    return context.CurrentResult;
                }
                while (count-- > 0){
                    try{
                        var apartmentState = GetApartmentState(context,_apartmentState);
                        
                        void ExecuteTest() {
                            if (Thread.CurrentThread.GetApartmentState() != apartmentState)
                                Task.Factory.StartTask(() => context.CurrentResult = innerCommand.Execute(context),
                                    thread => thread.SetApartmentState(apartmentState)).Wait();
                            else
                                innerCommand.Execute(context);
                        }

                        if (Debugger.IsAttached) {
                            ExecuteTest();
                        }
                        else {
                            Polly.Policy.Timeout(TimeSpan.FromMilliseconds(_timeout), TimeoutStrategy.Optimistic).Execute(ExecuteTest);
                            if (count <= 0) continue;
                            TestContext.Out.WriteWarning($"{context.CurrentResult.Message}");
                            TestContext.Out.WriteLine($"Retry {context.CurrentRepeatCount+1} of {_tryCount}");
                            if (context.CurrentResult.ResultState!=ResultState.Success){
                                context.CurrentRepeatCount++;
                            }
                            else{
                                break;
                            }
                        }
                    }
                    catch (Exception ex){
                        context.CurrentResult ??= context.CurrentTest.MakeTestResult();
                        var message = $" Timeout ({_timeout})ms, retry {context.CurrentRepeatCount+1} of {_tryCount}";
                        TestContext.Out.WriteWarning(message);
                        context.CurrentResult.RecordException(new Exception(message,ex));
                        if (count > 0){
                            if (context.CurrentResult.ResultState!=ResultState.Success){
                                context.CurrentRepeatCount++;
                            }
                            else{
                                break;
                            }
                        }
                    }

                    
                }

                return context.CurrentResult;
            }
            
            private static ApartmentState GetApartmentState(TestExecutionContext context, ApartmentState apartmentState) 
                => (ApartmentState?)context.CurrentTest.Method?.MethodInfo.Attribute<ApartmentAttribute>()?.Properties[PropertyNames.ApartmentState]
                    .OfType<object>().First() ?? apartmentState;
        }
    }
}