using System;
using System.ComponentModel;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Xpo.BaseObjects;

namespace Xpand.XAF.Modules.Workflow.BusinessObjects.Commands{
    public class CommandCategory(Session session) : XPCustomBaseObject(session){
        Guid _oid;

        [Key][Browsable(false)]
        public Guid Oid{
            get => _oid;
            set => SetPropertyValue(nameof(Oid), ref _oid, value);
        }

        public override void AfterConstruction(){
            base.AfterConstruction();
            ClassInfo.KeyProperty.SetValue(this,XpoDefault.NewGuid());
        }

        string _name;

        [RuleUniqueValue]
        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
        
        [Association("CommandCategory-Commands")]
        public XPCollection<WorkflowCommand> Commands => GetCollection<WorkflowCommand>();

    }
}