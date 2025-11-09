using System;
using System.ComponentModel;
using DevExpress.ExpressApp.Model;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Attributes.Custom;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Workflow.BusinessObjects.Commands{
    [OptimisticLocking(OptimisticLockingBehavior.NoLocking)]
    [ListViewShowFooter][DefaultProperty(nameof(Created))]
    public class CommandExecution(Session session) : CustomBaseObject(session){
        WorkflowCommand _workflowCommand;

        [Association("CommandObject-ActionObjectExecutions")]
        public WorkflowCommand WorkflowCommand{
            get => _workflowCommand;
            set => SetPropertyValue(nameof(WorkflowCommand), ref _workflowCommand, value);
        }

        
        DateTime _created;

        [DisplayDateAndTime][ColumnSummary(SummaryType.Count)]
        public DateTime Created{
            get => _created;
            set => SetPropertyValue(nameof(Created), ref _created, value);
        }
    }
}