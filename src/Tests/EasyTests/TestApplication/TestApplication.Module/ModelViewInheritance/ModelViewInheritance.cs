using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using Xpand.XAF.Modules.ModelViewInheritance;

namespace TestApplication.Module.ModelViewInheritance {
    [DefaultClassOptions]
    public class ModelViewInheritanceBaseObject:Person {
        public ModelViewInheritanceBaseObject(Session session) : base(session) { }
    }    

    [DefaultClassOptions][ModelMergedDifferences(typeof(ModelViewInheritanceBaseObject))]
    public class ModelViewInheritance:Person {
        public ModelViewInheritance(Session session) : base(session) { }
    }

}
