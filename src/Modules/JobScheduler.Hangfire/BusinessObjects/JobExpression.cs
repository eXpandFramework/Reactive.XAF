using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Fasterflect;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects {
    [DefaultClassOptions][NavigationItem("JobScheduler")]
    [DefaultProperty(nameof(Name))]
    public class JobExpression:CustomBaseObject {
        public JobExpression(Session session) : base(session) {
        }

        string _name;
        [RuleRequiredField][RuleUniqueValue]
        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
        ObjectType _jobType;

        [RuleRequiredField]
        [DataSourceProperty(nameof(JobTypes))][Size(SizeAttribute.Unlimited)]
        [ValueConverter(typeof(ObjectTypeValueConverter))][Persistent]
        public ObjectType JobType {
            get => _jobType;
            set {
                SetPropertyValue(nameof(JobType), ref _jobType, value);
                var name = JobType?.Type?.Name.FirstCharacterToLower();
                if (name != null && !$"{Expression}".StartsWith($"{name} =>")) {
                    Expression = name;
                }
            }
        }

        [Browsable(false)]
        public IList<ObjectType> JobTypes => AppDomain.CurrentDomain.JobMethods().Select(m=>m.DeclaringType).Distinct()
            .Select(type => new ObjectType(type){Name = type.Attribute<JobProviderAttribute>().DisplayName??type.Name.CompoundName()}).ToArray();

        string _expression;
        [RuleRequiredField]
        [Size(SizeAttribute.Unlimited)]
        public string Expression {
            get => _expression;
            set => SetPropertyValue(nameof(Expression), ref _expression, value);
        }
    }
}