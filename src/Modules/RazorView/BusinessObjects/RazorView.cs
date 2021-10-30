using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.RazorView.BusinessObjects {
    [DefaultClassOptions]
    [Appearance("Color Error",AppearanceItemType.ViewItem, nameof(Error)+"!=''",TargetItems = nameof(Error),FontColor = "Red")]
    [Appearance("Hide Error",AppearanceItemType.ViewItem, nameof(Error)+" Is null",TargetItems = nameof(Error),Visibility = ViewItemVisibility.Hide)]
    public class RazorView:CustomBaseObject {
        private ObjectType _modelType;
        public RazorView(Session session) : base(session) { }

        [DataSourceProperty(nameof(Objects))]
        [ValueConverter(typeof(ObjectTypeValueConverter))][RuleRequiredField]
        public ObjectType ModelType {
            get => _modelType;
            set => SetPropertyValue(nameof(ModelType), ref _modelType, value);
        }

        string _error;

        [Size(SizeAttribute.Unlimited)]
        public string Error {
            get => _error;
            internal set => SetPropertyValue(nameof(Error), ref _error, value);
        }

        string _modelCriteria;
        [CriteriaOptions(nameof(ModelType)+"."+nameof(ObjectType.Type))]
        [EditorAlias(EditorAliases.CriteriaPropertyEditor), Size(SizeAttribute.Unlimited)]
        public string ModelCriteria {
            get => _modelCriteria;
            set => SetPropertyValue(nameof(ModelCriteria), ref _modelCriteria, value);
        }
        
        [Browsable(false)]
        public IList<ObjectType> Objects 
            => CaptionHelper.ApplicationModel.BOModel.Where(c => c.TypeInfo.IsPersistent)
                .Select(modelClass => new ObjectType(modelClass.TypeInfo.Type) {Name = modelClass.Caption}).ToArray();
        

        string _template;

        [Size(SizeAttribute.Unlimited)]
        [RuleRequiredField]
        [ModelDefault("RowCount","30")]
        [ImmediatePostData]
        public string Template {
            get => _template;
            set => SetPropertyValue(nameof(Template), ref _template, value);
        }

        string _preview;

        [Size(SizeAttribute.Unlimited)][NonPersistent]
        [EditorAlias(EditorAliases.RichTextPropertyEditor)]
        public string Preview {
            get => _preview;
            internal set => SetPropertyValue(nameof(Preview), ref _preview, value);
        }
        
        string _name;

        [RuleRequiredField][RuleUniqueValue]
        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
        
    }
    
    
}