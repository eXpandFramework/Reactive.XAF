using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;

namespace Xpand.Extensions.XAF.Xpo.BaseObjects {
    [NonPersistent]
    public abstract class XPCustomBaseObject: XPCustomObject,IObjectSpaceLink{
	    protected XPCustomBaseObject() {
	    }

	    protected XPCustomBaseObject(Session session, XPClassInfo classInfo) : base(session, classInfo) {
	    }
        protected bool SetPropertyValue<T>(ref T oldValue, T newValue,[CallerMemberName]string caller=null) 
            => base.SetPropertyValue(caller, ref oldValue, newValue);
	    protected T GetSafe<T>(Func<T> func) => !IsLoading && !IsSaving ? func() : default;
        protected override void OnSaving() {
            if (TruncateStrings)
                DoTruncateStrings();
            base.OnSaving();
        }

        public override string ToString() {
            if (!_isDefaultPropertyAttributeInit) {
                if (ClassInfo.FindAttributeInfo(typeof(DefaultPropertyAttribute)) is DefaultPropertyAttribute attrib) {
                    _defaultPropertyMemberInfo = ClassInfo.FindMember(attrib.Name);
                }
                _isDefaultPropertyAttributeInit = true;
            }
            object obj = _defaultPropertyMemberInfo?.GetValue(this);
            if (obj != null) {
                return obj.ToString();
            }
            if (!_isDefaultPropertyAttributeInit) {
                string defaultPropertyName = string.Empty;
                var xafDefaultPropertyAttribute = XafTypesInfo.Instance.FindTypeInfo(GetType()).FindAttribute<XafDefaultPropertyAttribute>();
                if (xafDefaultPropertyAttribute != null) {
                    defaultPropertyName = xafDefaultPropertyAttribute.Name;
                } else {
                    var defaultPropertyAttribute = XafTypesInfo.Instance.FindTypeInfo(GetType()).FindAttribute<DefaultPropertyAttribute>();
                    if (defaultPropertyAttribute != null) {
                        defaultPropertyName = defaultPropertyAttribute.Name;
                    }
                }
                if (!string.IsNullOrEmpty(defaultPropertyName)) {
                    _defaultPropertyMemberInfo = ClassInfo.FindMember(defaultPropertyName);
                }
                _isDefaultPropertyAttributeInit = true;
            }
            obj = _defaultPropertyMemberInfo?.GetValue(this);
            return obj?.ToString() ?? base.ToString();
        }

        [Browsable(false)]
        [MemberDesignTimeVisibility(false)]
        public bool IsNewObject => Session.IsNewObject(this);

        [Obsolete("Use XpandUnitOfWork instead")]
        [NonPersistent]
        public HashSet<string> ChangedProperties { get; set; }

        protected override void TriggerObjectChanged(ObjectChangeEventArgs args) {
            if (!CancelTriggerObjectChanged)
                base.TriggerObjectChanged(args);
        }

        [Browsable(false)]
        [NonPersistent]
        [MemberDesignTimeVisibility(false)]
        public bool CancelTriggerObjectChanged { get; set; }

        [Browsable(false)]
        [NonPersistent]
        [MemberDesignTimeVisibility(false)]
        public bool TruncateStrings { get; set; }

        private void DoTruncateStrings() {
            foreach (XPMemberInfo xpMemberInfo in ClassInfo.PersistentProperties) {
                if (xpMemberInfo.MemberType == typeof(string)) {
                    if (xpMemberInfo.GetValue(this) is string value) {
                        value = TruncateValue(xpMemberInfo, value);
                        xpMemberInfo.SetValue(this, value);
                    }
                }
            }
        }
        string TruncateValue(XPMemberInfo xpMemberInfo, string value) {
            if (xpMemberInfo.HasAttribute(typeof(SizeAttribute))) {
                int size = ((SizeAttribute)xpMemberInfo.GetAttributeInfo(typeof(SizeAttribute))).Size;
                if (size > -1 && value.Length > size)
                    value = value.Substring(0, size - 1);
            } else if (value.Length > 99)
                value = value.Substring(0, 99);
            return value;
        }

        private bool _isDefaultPropertyAttributeInit;
        private XPMemberInfo _defaultPropertyMemberInfo;

        protected XPCustomBaseObject(Session session):base(session) {
            
        }

        [Browsable(false)]
        public IObjectSpace ObjectSpace{ get; set; }
    }
}