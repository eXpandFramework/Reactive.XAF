using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Xpo.BaseObjects;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects {
    [XafDefaultProperty(nameof(Name))][NavigationItem("JobScheduler")][DefaultClassOptions]
    public class CronExpression:XPCustomBaseObject {
        public CronExpression(Session session) : base(session) {
        }

        string _name;
        [Key][RuleRequiredField][RuleUniqueValue][VisibleInAllViews()]
        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        string _expression;
        [RuleRequiredField]
        public string Expression {
            get => _expression;
            set => SetPropertyValue(nameof(Expression), ref _expression, value);
        }
    }
}