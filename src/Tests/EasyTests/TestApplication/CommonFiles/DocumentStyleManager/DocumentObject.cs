using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

// ReSharper disable once CheckNamespace
namespace TestApplication.Office.DocumentStyleManager{
    
    public class DocumentObject:BaseObject{
        public DocumentObject(Session session) : base(session){
        }

        string _name;

        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        [Delayed(true)][EditorAlias(EditorAliases.RichTextPropertyEditor)]
        public byte[] Content{
            get => GetDelayedPropertyValue<byte[]>(nameof(Content));
            set => SetPropertyValue(nameof(Content), value);
        }
    }
}
