using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests{
    [DefaultProperty(nameof(Name))][DomainComponent]
    public class DataObject : BaseObject,IObjectSpaceLink{
        public DataObject(Session session) : base(session){
        }

        string _name;

        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
        [Delayed(true)][EditorAlias(EditorAliases.RichTextPropertyEditor)][RuleRequiredField(DefaultContexts.Save)]
        public byte[] Content{
            get => GetDelayedPropertyValue<byte[]>(nameof(Content));
            set => SetPropertyValue(nameof(Content), value);
        }

        public IObjectSpace ObjectSpace{ get; set; }
    }
    [DefaultProperty(nameof(Name))][DomainComponent]
    public class DataObjectParent : DataObject{
        public DataObjectParent(Session session) : base(session){
        }

        
    }
}