using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Workflow.BusinessObjects;
using Xpand.XAF.Modules.Workflow.BusinessObjects.Commands;
using Xpand.XAF.Modules.Workflow.Services;
using Xpand.XAF.Modules.Workflow.Tests.BOModel;
using Xpand.XAF.Modules.Workflow.Tests.Common;

namespace Xpand.XAF.Modules.Workflow.Tests {
    public class ObjectModifiedWorkflowCommandTests : BaseWorkflowTest {
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Basic_Execution_On_Modification() {
            await using var application = NewApplication();
            
            var capturedOutput = new ReplaySubject<object[]>();
            long oid = -1;

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var wf = space.CreateObject<WF>();
                    wf.Status = "Initial";
                    oid = wf.Oid;

                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ObjectModifiedWorkflowCommand>();
                    command.Object = command.Objects.First(s => s.Name == typeof(WF).FullName);
                    command.Member = command.Members.First(m => m.Name == nameof(WF.Status));
                    command.CommandSuite = suite;

                    return suite.Commit()
                        .SelectMany(_ => command.WhenExecuted().Do(objects => {
                            capturedOutput.OnNext(objects);
                            capturedOutput.OnCompleted();
                        }))
                        .To(suite);
                }))
                .Take(1)
                .Subscribe();
            WorkflowModule(application);
            
            await application.StartWinTest(frame => {
                    var space = frame.Application.CreateObjectSpace();
                    var wf = space.GetObjectByKey<WF>(oid);
                    wf.Status = "Modified";
                    return wf.Commit().ToUnit();
                })
                .Timeout(30.Seconds()).FirstOrDefaultAsync();

            var result = await capturedOutput.FirstAsync();
            result.Length.ShouldBe(1);
            var modifiedWf = result.Single().ShouldBeOfType<WF>();
            modifiedWf.Oid.ShouldBe(oid);
            modifiedWf.Status.ShouldBe("Modified");
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Criteria_Filtering_Positive_Case() {
            await using var application = NewApplication();
            
            var capturedOutput = new ReplaySubject<object[]>();
            long oid = -1;

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var wf = space.CreateObject<WF>();
                    wf.Status = "Initial";
                    oid = wf.Oid;

                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ObjectModifiedWorkflowCommand>();
                    command.Object = command.Objects.First(s => s.Name == typeof(WF).FullName);
                    command.Member = command.Members.First(m => m.Name == nameof(WF.Status));
                    command.Criteria = "[Status] = 'Modified'";
                    command.CommandSuite = suite;

                    return suite.Commit()
                        .SelectMany(_ => command.WhenExecuted().Do(objects => {
                            capturedOutput.OnNext(objects);
                            capturedOutput.OnCompleted();
                        }))
                        .To(suite);
                }))
                .Take(1)
                .Subscribe();
            WorkflowModule(application);
            
            await application.StartWinTest(frame => {
                    var space = frame.Application.CreateObjectSpace();
                    var wf = space.GetObjectByKey<WF>(oid);
                    wf.Status = "Modified";
                    return wf.Commit().ToUnit();
                })
                .Timeout(30.Seconds()).FirstOrDefaultAsync();

            var result = await capturedOutput.FirstAsync();
            result.Length.ShouldBe(1);
            var modifiedWf = result.Single().ShouldBeOfType<WF>();
            modifiedWf.Oid.ShouldBe(oid);
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task MemberName_Specificity() {
            await using var application = NewApplication();
            
            var executionCount = 0;
            long oid = -1;

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var wf = space.CreateObject<WF>();
                    oid = wf.Oid;

                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ObjectModifiedWorkflowCommand>();
                    command.Object = command.Objects.First(s => s.Name == typeof(WF).FullName);
                    command.Member = command.Members.First(m => m.Name == nameof(WF.Status));
                    command.CommandSuite = suite;

                    return suite.Commit()
                        .SelectMany(_ => command.WhenExecuted().Do(_ => executionCount++))
                        .To(suite);
                }))
                .Subscribe();
            WorkflowModule(application);
            
            await application.StartWinTest(frame => {
                    var space = frame.Application.CreateObjectSpace();
                    var wf = space.GetObjectByKey<WF>(oid);
                    wf.Name = "Modified";
                    return wf.Commit().ToUnit()
                        .Merge(Observable.Timer(200.Milliseconds()).ToUnit());
                })
                .Timeout(30.Seconds()).FirstOrDefaultAsync();

            executionCount.ShouldBe(0);
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task ObjectName_Specificity() {
            await using var application = NewApplication();
            
            var executionCount = 0;

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ObjectModifiedWorkflowCommand>();
                    command.Object = command.Objects.First(s => s.Name == typeof(WF).FullName);
                    command.Member = command.Members.First(m => m.Name == nameof(WF.Status));
                    command.CommandSuite = suite;

                    return suite.Commit()
                        .SelectMany(_ => command.WhenExecuted().Do(_ => executionCount++))
                        .To(suite);
                }))
                .Subscribe();
            WorkflowModule(application);
            
            await application.StartWinTest(frame => {
                    var space = frame.Application.CreateObjectSpace();
                    var otherTypeObject = space.CreateObject<TestCommand>();
                    otherTypeObject.Id = "Modified";
                    return otherTypeObject.Commit().ToUnit()
                        .Merge(Observable.Timer(200.Milliseconds()).ToUnit());
                })
                .Timeout(30.Seconds()).FirstOrDefaultAsync();

            executionCount.ShouldBe(0);
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Batching_with_BufferUntilInactive() {
            await using var application = NewApplication();
            
            var capturedOutput = new ReplaySubject<object[]>();
            long oid1 = -1;
            long oid2 = -1;

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var wf1 = space.CreateObject<WF>();
                    var wf2 = space.CreateObject<WF>();
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ObjectModifiedWorkflowCommand>();
                    command.Object = command.Objects.First(s => s.Name == typeof(WF).FullName);
                    command.Member = command.Members.First(m => m.Name == nameof(WF.Status));
                    command.CommandSuite = suite;

                    return suite.Commit()
                        .Do(_ => {
                            oid1 = wf1.Oid;
                            oid2 = wf2.Oid;
                        })
                        .SelectMany(_ => command.WhenExecuted().Take(2)
                            .Do(objects => {
                                capturedOutput.OnNext(objects);
                            },() => capturedOutput.OnCompleted()))
                            .To(suite);
                }))
                .Take(1)
                .Subscribe();
            WorkflowModule(application);
            
            await application.StartWinTest(frame => {
                    var space = frame.Application.CreateObjectSpace();
                    var wf1 = space.GetObjectByKey<WF>(oid1);
                    wf1.Status = "Modified1";
                    space.CommitChanges();
                    space = frame.Application.CreateObjectSpace();
                    var wf2 = space.GetObjectByKey<WF>(oid2);
                    wf2.Status = "Modified2";
                    return space.CommitChangesAsync().ToObservable().ToUnit();
                })
                .Timeout(30.Seconds()).FirstOrDefaultAsync();

            var result = await capturedOutput.FirstAsync();
            result.Length.ShouldBe(2);
            var resultOids = result.Cast<WF>().Select(wf => wf.Oid).ToArray();
            resultOids.ShouldContain(oid1);
            resultOids.ShouldContain(oid2);
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Output_Deduplication() {
            await using var application = NewApplication();
            
            var capturedOutput = new ReplaySubject<object[]>();
            long oid = -1;

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var wf = space.CreateObject<WF>();
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ObjectModifiedWorkflowCommand>();
                    command.Object = command.Objects.First(s => s.Name == typeof(WF).FullName);
                    command.Member = command.Members.First(m => m.Name == nameof(WF.Status));
                    command.CommandSuite = suite;

                    return suite.Commit()
                        .Do(_ => oid = wf.Oid)
                        .SelectMany(_ => command.WhenExecuted().Take(2).Do(objects => {
                            capturedOutput.OnNext(objects);
                            
                        },() => capturedOutput.OnCompleted()))
                        .To(suite);
                }))
                .Take(1)
                .Subscribe();
            WorkflowModule(application);
            
            await application.StartWinTest(frame => {
                    var space1 = frame.Application.CreateObjectSpace();
                    var wf1 = space1.GetObjectByKey<WF>(oid);
                    wf1.Status = "Modified1";
                    space1.CommitChanges();

                    var space2 = frame.Application.CreateObjectSpace();
                    var wf2 = space2.GetObjectByKey<WF>(oid);
                    wf2.Status = "Modified2";
                    return space2.CommitChangesAsync().ToObservable().ToUnit();
                })
                .Timeout(30.Seconds()).FirstOrDefaultAsync();

            var result = await capturedOutput.FirstAsync();
            result.Length.ShouldBe(1);
            var modifiedWf = result.Single().ShouldBeOfType<WF>();
            modifiedWf.Oid.ShouldBe(oid);
            modifiedWf.Status.ShouldBe("Modified2");
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Graceful_Exit_On_Null_ObjectName() {
            await using var application = NewApplication();
            
            var executionCount = 0;

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ObjectModifiedWorkflowCommand>();
                    command.CommandSuite = suite;

                    return suite.Commit()
                        .SelectMany(_ => command.WhenExecuted().Do(_ => executionCount++))
                        .To(suite);
                }))
                .Subscribe();
            WorkflowModule(application);
            
            await application.StartWinTest(frame => {
                    var space = frame.Application.CreateObjectSpace();
                    var wf = space.CreateObject<WF>();
                    wf.Status = "Modified";
                    return wf.Commit().ToUnit()
                        .Merge(Observable.Timer(200.Milliseconds()).ToUnit());
                })
                .Timeout(30.Seconds()).FirstOrDefaultAsync();

            executionCount.ShouldBe(0);
        }
    }
}