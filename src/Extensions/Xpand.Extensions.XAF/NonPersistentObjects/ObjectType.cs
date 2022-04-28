using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.Extensions.XAF.NonPersistentObjects {
    [DomainComponent]
    [DefaultProperty(nameof(Name))]
    public class ObjectType:NonPersistentBaseObject{
        private string _name;
        private Type _type;

        public ObjectType(Type type) {
            Type = type;
            Name = type?.Name.CompoundName();
        }

        // [DevExpress.ExpressApp.Data.Key]
        [VisibleInAllViews]
        // [IgnoreDataMember]
        public string Name {
            get => _name;
            set {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        [Browsable(false)]
        public Type Type {
            get => _type;
            set {
                if (value == _type) return;
                _type = value;
                OnPropertyChanged();
            }
        }
    }
}