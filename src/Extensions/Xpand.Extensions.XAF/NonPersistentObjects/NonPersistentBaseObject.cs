using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;

using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.TypesInfoExtensions;

namespace Xpand.Extensions.XAF.NonPersistentObjects {
    public abstract class NonPersistentBaseObject:DevExpress.ExpressApp.NonPersistentBaseObject,IReloadWhenChange {
        public event EventHandler<EventArgs> ObjectSpaceChanged;
        private bool _isDefaultPropertyAttributeInit;
        private IMemberInfo _defaultPropertyMemberInfo;
        
        [SuppressMessage("ReSharper", "OptionalParameterHierarchyMismatch")]
        protected override void OnPropertyChanged([CallerMemberName] string memberName = "") => base.OnPropertyChanged(memberName);

        [Browsable(false)][Newtonsoft.Json.JsonIgnore]
        public new IObjectSpace ObjectSpace => base.ObjectSpace; 
        public override string ToString() {
            if(!_isDefaultPropertyAttributeInit) {
                string defaultPropertyName = string.Empty;
                var info = XafTypesInfo.Instance.FindTypeInfo(GetType());
                var xafDefaultPropertyAttribute = info.FindAttribute<XafDefaultPropertyAttribute>();
                if(xafDefaultPropertyAttribute != null) {
                    defaultPropertyName = xafDefaultPropertyAttribute.Name;
                } else {
                    var defaultPropertyAttribute = info.FindAttribute<DefaultPropertyAttribute>();
                    if(defaultPropertyAttribute != null) {
                        defaultPropertyName = defaultPropertyAttribute.Name;
                    }
                }
                if(!string.IsNullOrEmpty(defaultPropertyName)) {
                    _defaultPropertyMemberInfo = GetType().ToTypeInfo().FindMember(defaultPropertyName);
                }
                _isDefaultPropertyAttributeInit = true;
            }
            if(_defaultPropertyMemberInfo != null) {
                try {
                    return _defaultPropertyMemberInfo.GetValue(this)?.ToString();
                }
                catch {
                    // ignored
                }
            }

            return base.ToString();
        }
        
        protected bool SetPropertyValue<T>(string propertyName ,ref T propertyValue, T newValue ) 
            => base.SetPropertyValue(ref propertyValue, newValue, propertyName);

        protected override void OnObjectSpaceChanged() {
            base.OnObjectSpaceChanged();
            if (ObjectSpace != null) {
                OnObjectSpaceChanged(EventArgs.Empty);    
            }
        }

        protected virtual void OnObjectSpaceChanged(EventArgs e) => ObjectSpaceChanged?.Invoke(this, e);

        
        Action<string> IReloadWhenChange.WhenPropertyChanged => OnPropertyChanged;
    }

}
