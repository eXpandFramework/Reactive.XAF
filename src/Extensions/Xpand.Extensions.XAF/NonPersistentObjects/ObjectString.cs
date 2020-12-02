using System.ComponentModel;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.NonPersistentObjects {
    [XafDefaultProperty(nameof(Caption))]
    [DomainComponent]
    public class ObjectString  {
        public ObjectString(string name) {
            Name = name;
            Caption = name;
        }

        public string Caption{ get; set; }
        [DevExpress.ExpressApp.Data.Key][Browsable(false)]
        public string Name{ get; set; }

        public static implicit operator string(ObjectString objectString) {
            return objectString?.Name;
        }
    }
}