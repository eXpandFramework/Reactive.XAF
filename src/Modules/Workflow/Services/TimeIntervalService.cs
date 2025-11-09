
using System;
using System.Linq;
using System.Reactive.Linq;
using Fasterflect;
using Humanizer;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.StringExtensions;
using Xpand.XAF.Modules.Workflow.BusinessObjects.Commands;

namespace Xpand.XAF.Modules.Workflow.Services{
    internal static class TimeIntervalService{
        

        internal static IObservable<object[]> WhenExecute(this TimeIntervalWorkflowCommand workflowCommand) 
            => workflowCommand.HourlyIntervals.IsNotNullOrEmpty()
                ? $"{workflowCommand.HourlyIntervals}".Split(',').ToNowObservable()
                    .Select(hr => (double.Parse(hr)).Hours().Timer()
                        .SelectMany(_ => workflowCommand.Interval.Interval(emitNow:true).Select(x => new object[]{x})))
                    .Switch()
                : workflowCommand.RandomStart(workflowCommand.Mode==TriggerAction.Random);

        private static IObservable<object[]> RandomStart(this TimeIntervalWorkflowCommand workflowCommand,bool random){
            if (random){
                return TimeSpan.Zero.Timer().StepRandomInterval(maxDelay: workflowCommand.Interval, minDelay: TimeSpan.Zero)
                    .SelectMany(_ => workflowCommand.RandomStart(false));
            }
            if (workflowCommand.Mode == TriggerAction.Persistent){
                var lastExecutionTime = workflowCommand.CommandSuite.Commands
                    .SelectMany(command1 => command1.Executions)
                    .OrderByDescending(execution => execution.Created)
                    .FirstOrDefault()?.Created;

                if (lastExecutionTime.HasValue){
                    var nextExecutionTime = lastExecutionTime.Value.Add(workflowCommand.Interval);
                    var delay = nextExecutionTime.Subtract(DateTime.Now);
                    return delay <= TimeSpan.Zero||workflowCommand.EmitNow
                        ? workflowCommand.Interval.Interval(true).Select(x => new object[]{ x })
                        : delay.Timer().SelectMany(_ => workflowCommand.Interval.Interval(true).Select(x => new object[]{ x }));
                }
                return workflowCommand.Interval.Interval(workflowCommand.EmitNow).Select(x => new object[]{x});
            }
            return workflowCommand.Interval.Interval(workflowCommand.EmitNow).Select(x => new object[]{x});
        }


            
        public static DateTime? NextEmission(this TimeIntervalWorkflowCommand workflowCommand){
            if (workflowCommand.IsDeleted||workflowCommand.Interval == TimeSpan.Zero && string.IsNullOrEmpty(workflowCommand.HourlyIntervals)||(bool)workflowCommand.GetPropertyValue("IsInvalidated")) return null;
            if (!string.IsNullOrEmpty(workflowCommand.HourlyIntervals)) {
                var now = DateTime.Now;
                var today = now.Date;
            
                var nextEmissionToday = workflowCommand.HourlyIntervals.Split(',')
                    .Select(s => double.TryParse(s.Trim(), out var d) ? (double?)d : null)
                    .Where(d => d.HasValue)
                    .Select(d => today.AddHours(d.Value))
                    .OrderBy(dt => dt)
                    .FirstOrDefault(dt => dt > now);

                return nextEmissionToday != default ? nextEmissionToday
                    : workflowCommand.HourlyIntervals.Split(',')
                        .Select(s => double.TryParse(s.Trim(), out var d) ? (double?)d : null)
                        .Where(d => d.HasValue)
                        .Select(d => today.AddDays(1).AddHours(d.Value))
                        .OrderBy(dt => dt)
                        .FirstOrDefault();
            }

            switch (workflowCommand.Mode) {
                case TriggerAction.Random:
                    return null;
                case TriggerAction.Persistent:
                case TriggerAction.Startup:
                    if (workflowCommand.Mode != TriggerAction.Persistent)
                        return workflowCommand.EmitNow ? DateTime.Now : DateTime.Now.Add(workflowCommand.Interval);
                    var lastExecutionTime = workflowCommand.CommandSuite.Commands
                        .SelectMany(command1 => command1.Executions)
                        .OrderByDescending(execution => execution.Created)
                        .FirstOrDefault()?.Created;
                    return lastExecutionTime?.Add(workflowCommand.Interval) ?? (workflowCommand.EmitNow ? DateTime.Now : DateTime.Now.Add(workflowCommand.Interval));

                default:
                    return null;
            }
        }

    
    }
}