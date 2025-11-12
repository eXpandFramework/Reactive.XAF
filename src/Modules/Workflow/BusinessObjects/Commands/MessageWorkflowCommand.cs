using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Humanizer;
using Xpand.XAF.Modules.Workflow.Services;

namespace Xpand.XAF.Modules.Workflow.BusinessObjects.Commands{
    [DefaultProperty(nameof(Description))]
    [System.ComponentModel.DisplayName("Message")]
    [ImageName("Message")][OptimisticLocking(OptimisticLockingBehavior.NoLocking)]
    public class MessageWorkflowCommand(Session session) :WorkflowCommand(session){
        InformationType _msgType;

        public override void AfterConstruction(){
            base.AfterConstruction();
            MsgType=InformationType.Info;
            DisplayFor = 1.Days();
            Position=InformationPosition.Right;
        }

        public InformationType MsgType{
            get => _msgType;
            set => SetPropertyValue(ref _msgType, value);
        }

        InformationPosition _position;

        public InformationPosition Position{
            get => _position;
            set => SetPropertyValue(ref _position, value);
        }
        TimeSpan _displayFor;
        
        public TimeSpan DisplayFor{
            get => _displayFor;
            set => SetPropertyValue(ref _displayFor, value);
        }

        
        
        public override IObservable<object[]> Execute(XafApplication application, params object[] objects) 
            => this.ShowMessage( MsgType,Position, (int)DisplayFor.TotalMilliseconds,objects) ;
    }
}
