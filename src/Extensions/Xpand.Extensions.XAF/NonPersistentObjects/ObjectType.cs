using System;
using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.Extensions.XAF.NonPersistentObjects {
    [DomainComponent]
    [XafDefaultProperty(nameof(Name))]
    public class ObjectType{
        public ObjectType(Type type) {
            Type = type;
            Name = type?.Name.CompoundName();
        }

        [DevExpress.ExpressApp.Data.Key]
        public string Name{ get; set; }
        [Browsable(false)]
        public Type Type{ get; set; }
    }
}