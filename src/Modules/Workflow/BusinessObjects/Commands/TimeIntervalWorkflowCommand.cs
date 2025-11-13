using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Attributes.Appearance;
using Xpand.Extensions.XAF.Attributes.Validation;
using Xpand.XAF.Modules.Workflow.Services;

namespace Xpand.XAF.Modules.Workflow.BusinessObjects.Commands{
    [DefaultProperty(nameof(Description))]
    [System.ComponentModel.DisplayName("TimeInterval")]
    [ImageName("TimeIntervalWorkflowCommand")]
    [OptimisticLocking(OptimisticLockingBehavior.NoLocking)]
    [AppearanceToolTip("Disable " +nameof(LogExecutions)+ " when " + nameof(TriggerAction.Persistent),nameof(Mode)+"='"+nameof(TriggerAction.Persistent)+"'",TargetItems = nameof(LogExecutions),Enabled = false)]
    public class TimeIntervalWorkflowCommand(Session session) :WorkflowCommand(session){
        private TimeSpan _interval;
        private string _hourlyIntervals;
        private bool _emitNow;
        private TriggerAction _mode;
        public override string GetStartPath() => null;
        public override bool ShouldLogExecutions() => base.ShouldLogExecutions()&&Mode==TriggerAction.Persistent;

        protected override bool GetNeedSubscription() => false;

        protected override Type GetReturnType() => typeof(int);

        [ToolTip("The recurring time delay between command executions. This value is used by the 'Startup' and 'Persistent' modes, and for subsequent executions in 'Random' and 'HourlyIntervals' modes.")]
        [TimeSpanNotZero()]
        public TimeSpan Interval{
            get => _interval;
            set{
                if (SetPropertyValue(ref _interval, value)){
                    OnChanged(nameof(NextEmission));
                }
            }
        }

        [ToolTip("A comma-separated list of values representing durations in hours (e.g., '0.5, 1.5' for 30 and 90 minutes). The command will execute after the specified duration elapses. Note: Only the last duration in the list is used for scheduling the execution.")]

        public string HourlyIntervals{
            get => _hourlyIntervals;
            set{
                if (SetPropertyValue(nameof(HourlyIntervals), ref _hourlyIntervals, value)){
                    OnChanged(nameof(NextEmission));
                }
            }
        }

        [ToolTip("If true for 'Startup' or 'Persistent' modes, the first execution occurs immediately upon activation, rather than after the first interval. This setting is ignored if 'Hourly Intervals' is used.")]
        [AppearanceToolTip("Disable when "+nameof(HourlyIntervals),Criteria = nameof(HourlyIntervals)+" is Not null",Enabled = false)]
        public bool EmitNow{
            get => _emitNow;
            set{
                if (SetPropertyValue(nameof(EmitNow), ref _emitNow, value)){
                    OnChanged(nameof(NextEmission));
                }
            }
        }

        [ToolTip("'Startup': Executes based on the 'Interval' after the application starts. 'Random': Adds an initial random delay (up to the 'Interval' duration) before starting the regular interval timer. 'Persistent': Calculates the next execution time based on the timestamp of the last recorded execution, allowing workflows to resume after a restart.")]
        public TriggerAction Mode{
            get => _mode;
            set{
                if (SetPropertyValue(nameof(Mode), ref _mode, value)){
                    OnChanged(nameof(NextEmission));
                }
            }
        }

        [ToolTip("A read-only, calculated property showing the next scheduled execution time based on the current settings. Returns null for 'Random' mode because the start time is unpredictable.")]
        [Humanize()]
        public DateTime? NextEmission => this.NextEmission();

        public override IObservable<object[]> Execute(XafApplication application, params object[] objects) 
            => this.WhenExecute();

    }

    public enum TriggerAction{
        Startup,
        Random,
        Persistent
    }
}
