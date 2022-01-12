using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.TypesInfoExtensions;

namespace Xpand.Extensions.XAF.NonPersistentObjects {
    public abstract class NonPersistentBaseObject:DevExpress.ExpressApp.NonPersistentBaseObject {
        public event EventHandler<EventArgs> ObjectSpaceChanged;
        private bool _isDefaultPropertyAttributeInit;
        private IMemberInfo _defaultPropertyMemberInfo;
        [NotifyPropertyChangedInvocator]
        [SuppressMessage("ReSharper", "OptionalParameterHierarchyMismatch")]
        protected override void OnPropertyChanged([CallerMemberName] string memberName = "") => base.OnPropertyChanged(memberName);

        public override string ToString() {
            if(!_isDefaultPropertyAttributeInit) {
                string defaultPropertyName = string.Empty;
                XafDefaultPropertyAttribute xafDefaultPropertyAttribute = XafTypesInfo.Instance.FindTypeInfo(GetType()).FindAttribute<XafDefaultPropertyAttribute>();
                if(xafDefaultPropertyAttribute != null) {
                    defaultPropertyName = xafDefaultPropertyAttribute.Name;
                } else {
                    DefaultPropertyAttribute defaultPropertyAttribute = XafTypesInfo.Instance.FindTypeInfo(GetType()).FindAttribute<DefaultPropertyAttribute>();
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
            OnObjectSpaceChanged(EventArgs.Empty);
        }

        protected virtual void OnObjectSpaceChanged(EventArgs e) => ObjectSpaceChanged?.Invoke(this, e);
    }

}
