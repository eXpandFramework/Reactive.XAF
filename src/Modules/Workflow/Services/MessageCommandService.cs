using System;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Workflow.BusinessObjects.Commands;

namespace Xpand.XAF.Modules.Workflow.Services {
    public static class MessageCommandService {
        private static int DisplayInterval(this MessageWorkflowCommand workflowCommand) 
            => (int)(workflowCommand.DisplayFor>TimeSpan.Zero?workflowCommand.DisplayFor.Value.TotalMilliseconds: 10.ToSeconds().Milliseconds);
        
        internal static IObservable<object[]> InvokeMessageWorkflowCommand(this MessageWorkflowCommand workflowCommand, InformationType msgType,
            InformationPosition position,  object[] objects, bool verboseNotification) {
            LogFast($"Entering {nameof(InvokeMessageWorkflowCommand)} for command '{workflowCommand}' with {objects.Length} objects.");
            return Observable.Defer(() => {
                    objects = objects.ManySelect().ToArray();
                    var defaultPropertyObjects = objects.OfType<IDefaultProperty>().ToArray();
                    return objects.Except(defaultPropertyObjects).JoinDotNewLine().Observe().WhenNotDefault().WhenNotEmpty()
                        .Merge(defaultPropertyObjects.Select(property => property.DefaultPropertyValue).ToNowObservable())
                        .Select(o => {
                            var suiteMsg = $"Suite: {workflowCommand.CommandSuite}".EncloseHTMLImportant();
                            var commandMsg = $"Command: {((IDefaultProperty)workflowCommand).DefaultPropertyValue}".EncloseHTMLTag("i");
                            var objectMsg = $"{o}";
                            if (verboseNotification) {
                                objectMsg = $"{objects[0].GetType().Name}: ".EncloseHTMLTag("i") + objectMsg;
                                return new[] { suiteMsg, commandMsg, objectMsg }.JoinNewLine();
                            }

                            return objectMsg;
                        })
                        .Publish(shared => {
                            LogFast($"Publishing message content to different channels.");
                            return shared.ShowXafMessage(msgType, workflowCommand.DisplayInterval(), position, null);
                        })
                        .To<object[]>();
                }).ContinueOnFault(context: [nameof(InvokeMessageWorkflowCommand), workflowCommand.ToString()])
                .Finally(() => LogFast($"Exiting {nameof(InvokeMessageWorkflowCommand)} for command '{workflowCommand}'"))
                .IgnoreElements()
                .Concat(objects.Observe());
        }
    }
}