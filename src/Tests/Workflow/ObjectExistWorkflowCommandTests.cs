using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
    public class ObjectExistWorkflowCommandTests : BaseWorkflowTest {
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task SearchMode_Default_Emits_Existing_And_Committed_Objects() {
            await using var application = NewApplication();
            

            var capturedOutputs = new List<object[]>();
            long existingOid = -1;
            long newOid = -1;

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var existingWf = space.CreateObject<WF>();
                    existingWf.Status = "Pending";
                    existingOid = existingWf.Oid;

                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ObjectExistWorkflowCommand>();
                    command.Object = command.ObjectTypes.First(s => s.Name == typeof(WF).FullName);
                    command.Criteria = "[Status] = 'Pending'";
                    command.SearchMode = CommandSearchMode.Default;
                    command.CommandSuite = suite;

                    return suite.Commit()
                        .SelectMany(_ => command.WhenExecuted().Do(capturedOutputs.Add))
                        .To(suite);
                }))
                .Take(2)
                .Subscribe();
            WorkflowModule(application);
            
            
            await application.StartWinTest(frame => {
                    var space = frame.View.ObjectSpace;
                    var newWf = space.CreateObject<WF>();
                    newWf.Status = "Pending";
                    return newWf.Commit().Do(wf => newOid = wf.Oid);
                }) ;

            capturedOutputs.Count.ShouldBe(2);
            
            var firstEmission = capturedOutputs[0];
            firstEmission.Length.ShouldBe(1);
            var emittedExisting = firstEmission.Single().ShouldBeOfType<WF>();
            emittedExisting.Oid.ShouldBe(existingOid);

            var secondEmission = capturedOutputs[1];
            secondEmission.Length.ShouldBe(1);
            var emittedNew = secondEmission.Single().ShouldBeOfType<WF>();
            emittedNew.Oid.ShouldBe(newOid);
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task SearchMode_Existing_Emits_Only_Existing_Objects() {
            await using var application = NewApplication();
            
            var capturedOutputs = new List<object[]>();
            long existingOid = -1;

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var existingWf = space.CreateObject<WF>();
                    existingWf.Status = "Pending";
                    existingOid = existingWf.Oid;

                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ObjectExistWorkflowCommand>();
                    command.Object = command.ObjectTypes.First(s => s.Name == typeof(WF).FullName);
                    command.Criteria = "[Status] = 'Pending'";
                    command.SearchMode = CommandSearchMode.Existing;
                    command.CommandSuite = suite;

                    return suite.Commit()
                        .SelectMany(_ => command.WhenExecuted().Do(capturedOutputs.Add))
                        .To(suite);
                }))
                .Subscribe();
            WorkflowModule(application);
            
            await application.StartWinTest(frame => {
                    var space = frame.View.ObjectSpace;
                    var newWf = space.CreateObject<WF>();
                    newWf.Status = "Pending";
                    return newWf.Commit().ToUnit()
                        .Merge(Observable.Timer(200.Milliseconds()).ToUnit());
                });

            capturedOutputs.Count.ShouldBe(1);
            
            var firstEmission = capturedOutputs[0];
            firstEmission.Length.ShouldBe(1);
            var emittedExisting = firstEmission.Single().ShouldBeOfType<WF>();
            emittedExisting.Oid.ShouldBe(existingOid);
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task SearchMode_Commits_Emits_Only_Committed_Objects() {
            await using var application = NewApplication();
            
            var capturedOutputs = new List<object[]>();
            long newOid = -1;

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var existingWf = space.CreateObject<WF>();
                    existingWf.Status = "Pending";

                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ObjectExistWorkflowCommand>();
                    command.Object = command.ObjectTypes.First(s => s.Name == typeof(WF).FullName);
                    command.Criteria = "[Status] = 'Pending'";
                    command.SearchMode = CommandSearchMode.Commits;
                    command.CommandSuite = suite;

                    return suite.Commit()
                        .SelectMany(_ => command.WhenExecuted().Do(capturedOutputs.Add))
                        .To(suite);
                }))
                .Take(1)
                .Subscribe();
            WorkflowModule(application);
            
            await application.StartWinTest(frame => {
                    var space = frame.View.ObjectSpace;
                    var newWf = space.CreateObject<WF>();
                    newWf.Status = "Pending";
                    return newWf.Commit().Do(wf => newOid = wf.Oid);
                });

            capturedOutputs.Count.ShouldBe(1);
            
            var emission = capturedOutputs[0];
            emission.Length.ShouldBe(1);
            var emittedNew = emission.Single().ShouldBeOfType<WF>();
            emittedNew.Oid.ShouldBe(newOid);
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Combined_Criteria_And_InputFilterProperty_Filters_Results() {
            await using var application = NewApplication();
            
            var capturedOutput = new ReplaySubject<object[]>();

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var wfToFind = space.CreateObject<WF>();
                    wfToFind.Status = "Active";

                    var wfToIgnore = space.CreateObject<WF>();
                    wfToIgnore.Status = "Active";

                    var anotherWfToFind = space.CreateObject<WF>();
                    anotherWfToFind.Status = "Inactive";

                    var suite = space.CreateObject<CommandSuite>();
                    
                    var triggerCommand = space.CreateObject<TestCommand>();
                    triggerCommand.Id = "CombinedFilterTrigger";
                    triggerCommand.CommandSuite = suite;
                    
                    var targetCommand = space.CreateObject<ObjectExistWorkflowCommand>();
                    targetCommand.Object = targetCommand.ObjectTypes.First(s => s.Name == typeof(WF).FullName);
                    targetCommand.Criteria = "[Status] = 'Active'";
                    targetCommand.InputFilterProperty = nameof(WF.Oid);
                    targetCommand.StartAction = triggerCommand;
                    targetCommand.CommandSuite = suite;

                    return suite.Commit()
                        .SelectMany(_ => targetCommand.WhenExecuted().Do(objects => {
                            capturedOutput.OnNext(objects);
                            capturedOutput.OnCompleted();
                        }))
                        .To(suite);
                }))
                .Take(1)
                .Subscribe();
            WorkflowModule(application);
            
            await application.StartWinTest(_ => capturedOutput.ToUnit()).FirstOrDefaultAsync();

            var result = await capturedOutput.FirstAsync();
            result.Length.ShouldBe(2);
        }
        
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Simple_Property_Projection() {
            await using var application = NewApplication();
            
            var capturedOutput = new ReplaySubject<object[]>();
            long oid = -1;

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var wf = space.CreateObject<WF>();
                    oid = wf.Oid;

                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ObjectExistWorkflowCommand>();
                    command.Object = command.ObjectTypes.First(s => s.Name == typeof(WF).FullName);
                    command.OutputProperty = nameof(WF.Oid);
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
            
            await application.StartWinTest(_ => capturedOutput.ToUnit()).FirstOrDefaultAsync();

            var result = await capturedOutput.FirstAsync();
            result.Length.ShouldBe(1);
            result.Single().ShouldBe(oid);
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task Formatted_String_Projection() {
            await using var application = NewApplication();
            
            var capturedOutput = new ReplaySubject<object[]>();
            long oid = -1;
            const string status = "Active";

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var wf = space.CreateObject<WF>();
                    wf.Status = status;
                    oid = wf.Oid;

                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ObjectExistWorkflowCommand>();
                    command.Object = command.ObjectTypes.First(s => s.Name == typeof(WF).FullName);
                    command.OutputProperty = "ID: {Oid}, Status: {Status}";
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
            
            await application.StartWinTest(_ => capturedOutput.ToUnit())
                .FirstOrDefaultAsync();

            var result = await capturedOutput.FirstAsync();
            result.Length.ShouldBe(1);
            result.Single().ShouldBe($"ID: {oid}, Status: {status}");
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task TopReturnObjects_Validation() {
            await using var application = NewApplication();
            
            var capturedOutput = new ReplaySubject<object[]>();

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    space.CreateObject<WF>();
                    space.CreateObject<WF>();
                    space.CreateObject<WF>();

                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ObjectExistWorkflowCommand>();
                    command.Object = command.ObjectTypes.First(s => s.Name == typeof(WF).FullName);
                    command.TopReturnObjects = 2;
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
            
            await application.StartWinTest(_ => capturedOutput.ToUnit())
                .FirstOrDefaultAsync();

            var result = await capturedOutput.FirstAsync();
            result.Length.ShouldBe(2);
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task SkipTopReturnObjects_Validation() {
            await using var application = NewApplication();
            
            var capturedOutput = new ReplaySubject<object[]>();
            var oids = new List<long>();

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    var wf1 = space.CreateObject<WF>();
                    var wf2 = space.CreateObject<WF>();
                    var wf3 = space.CreateObject<WF>();
                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ObjectExistWorkflowCommand>();
                    command.Object = command.ObjectTypes.First(s => s.Name == typeof(WF).FullName);
                    command.SkipTopReturnObjects = 1;
                    command.CommandSuite = suite;
                    var property = space.CreateObject<CommandSortProperty>();
                    property.Name = nameof(WF.Oid);
                    command.SortProperties.Add(property);
                    return suite.Commit()
                        .Do(_ => {
                            oids.Add(wf1.Oid);
                            oids.Add(wf2.Oid);
                            oids.Add(wf3.Oid);
                        })
                        .SelectMany(_ => command.WhenExecuted()
                            .Do(objects => {
                                capturedOutput.OnNext(objects);
                                capturedOutput.OnCompleted();
                            }))
                            .To(suite);
                }))
                .Take(1)
                .Subscribe();
            WorkflowModule(application);
            
            await application.StartWinTest(_ => capturedOutput.ToUnit())
                .FirstOrDefaultAsync();

            var result = await capturedOutput.FirstAsync();
            result.Length.ShouldBe(2);
            var resultOids = result.Cast<WF>().Select(wf => wf.Oid).ToArray();
            resultOids.ShouldBe(oids.Skip(1));
        }
        
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task SortProperties_Validation() {
            await using var application = NewApplication();
            
            var capturedOutput = new ReplaySubject<object[]>();

            application.WhenSetupComplete()
                .SelectMany(_ => application.UseProviderObjectSpace(space => {
                    space.CreateObject<WF>().Status = "C";
                    space.CreateObject<WF>().Status = "A";
                    space.CreateObject<WF>().Status = "B";

                    var suite = space.CreateObject<CommandSuite>();
                    var command = space.CreateObject<ObjectExistWorkflowCommand>();
                    command.Object = command.ObjectTypes.First(s => s.Name == typeof(WF).FullName);
                    
                    var property = space.CreateObject<CommandSortProperty>();
                    property.Name = nameof(WF.Status);
                    command.SortProperties.Add(property);
                    
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
            
            await application.StartWinTest(_ => capturedOutput.ToUnit())
                .FirstOrDefaultAsync();

            var result = await capturedOutput.FirstAsync();
            result.Length.ShouldBe(3);
            var resultStatuses = result.Cast<WF>().Select(wf => wf.Status).ToArray();
            resultStatuses.ShouldBe(["A", "B", "C"]);
        }
        
    }
    
    


}