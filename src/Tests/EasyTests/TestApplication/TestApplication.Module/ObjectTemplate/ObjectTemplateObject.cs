using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace TestApplication.Module.ObjectTemplate {
    [DefaultClassOptions]
    public class ObjectTemplateObject:CustomBaseObject {
        public ObjectTemplateObject(Session session) : base(session) { }
    }
}