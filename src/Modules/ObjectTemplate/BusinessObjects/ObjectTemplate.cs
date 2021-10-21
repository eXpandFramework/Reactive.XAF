using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.ObjectTemplate.BusinessObjects {
    [DefaultClassOptions]
    public class ObjectTemplate:CustomBaseObject {
        private ObjectType _object;
        public ObjectTemplate(Session session) : base(session) { }

        [DataSourceProperty(nameof(Objects))]
        [ValueConverter(typeof(ObjectTypeValueConverter))]
        [Persistent]
        public ObjectType Object {
            get => _object;
            set {
                if (SetPropertyValue(nameof(Object), ref _object, value)) {
                    OnChanged(nameof(View));
                }
            }
        }
        [Browsable(false)]
        public IList<ObjectType> Objects 
            => CaptionHelper.ApplicationModel.BOModel.Where(c => c.TypeInfo.IsPersistent)
                .Select(modelClass => new ObjectType(modelClass.TypeInfo.Type) {Name = modelClass.Caption}).ToArray();
        

        string _template;

        [Size(SizeAttribute.Unlimited)]
        [RuleRequiredField]
        public string Template {
            get => _template;
            set => SetPropertyValue(nameof(Template), ref _template, value);
        }

        string _name;

        [RuleRequiredField][RuleUniqueValue]
        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
        
    }

    public enum MonitorContext {
        Application,Universal
    }

    
}