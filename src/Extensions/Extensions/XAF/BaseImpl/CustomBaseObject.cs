using System;
using System.Collections.Generic;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;

// ReSharper disable once CheckNamespace
namespace Xpand.XAF.Persistent.BaseImpl{
    [NonPersistent]
    public abstract class CustomBaseObject : XPCustomObject {
                
        [Persistent("Oid"), Key(true), VisibleInListView(false), VisibleInDetailView(false), VisibleInLookupListView(false), MemberDesignTimeVisibility(false)]
        private Guid _oid = Guid.Empty;
        [PersistentAlias("oid"), VisibleInListView(false), VisibleInDetailView(false), VisibleInLookupListView(false)]
        public Guid Oid => _oid;

        protected override void OnSaving() {
            if (TrucateStrings)
                DoTrucateStrings();
            base.OnSaving();
            if (Session is NestedUnitOfWork || !Session.IsNewObject(this) || !_oid.Equals(Guid.Empty))
                return;
            _oid = XpoDefault.NewGuid();
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
        public bool TrucateStrings { get; set; }

        private void DoTrucateStrings() {
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

        protected CustomBaseObject(Session session):base(session){
            
        }

        public override void AfterConstruction(){
            base.AfterConstruction();
            _oid = XpoDefault.NewGuid();
        }

        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
    }

}
