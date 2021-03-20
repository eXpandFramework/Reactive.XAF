#if XAF192
using DevExpress.ExpressApp.Data;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
namespace DevExpress.ExpressApp {
	public interface ICustomPropertyStore {
		object GetCustomPropertyValue(IMemberInfo memberInfo);
		bool SetCustomPropertyValue(IMemberInfo memberInfo, object value);
		bool UpdateCalculatedPropertiesOnChanged { get; set; }
	}
	[DomainComponent]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public abstract class NonPersistentEntityObject : IXafEntityObject, INotifyPropertyChanged, ICustomPropertyStore {
		[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
		public static Boolean DefaultUpdateCalculatedPropertiesOnChanged = true;
		private static Dictionary<Type, object> defaultValues;
		private Boolean updateCalculatedPropertiesOnChanged = DefaultUpdateCalculatedPropertiesOnChanged;
		private Dictionary<IMemberInfo, object> customPropertyStore;
		private Lazy<ITypeInfo> typeInfo;
		public NonPersistentEntityObject() {
			typeInfo = new Lazy<ITypeInfo>(GetTypeInfo);
		}
		protected virtual ITypeInfo GetTypeInfo() {
			return XafTypesInfo.Instance.FindTypeInfo(GetType());
		}
		#region IXafEntityObject
		public virtual void OnCreated() {
		}
		public virtual void OnSaving() {
		}
		public virtual void OnLoaded() {
		}
		#endregion
		#region INotifyPropertyChanged
		protected virtual void OnPropertyChanged(String propertyName) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		protected bool SetPropertyValue<T>(ref T propertyValue, T newValue, [CallerMemberName]string propertyName = null) {
			if(EqualityComparer<T>.Default.Equals(propertyValue, newValue)) {
				return false;
			}
			propertyValue = newValue;
			OnPropertyChanged(propertyName);
			return true;
		}
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion
		#region ICustomPropertyStore
		private Dictionary<IMemberInfo, object> CustomPropertyStore {
			get {
				if(customPropertyStore == null)
					customPropertyStore = new Dictionary<IMemberInfo, object>();
				return customPropertyStore;
			}
		}
		private object CreateDefValue(IMemberInfo memberInfo) {
			object result = null;
			if(memberInfo.MemberTypeInfo.IsValueType) {
				if (defaultValues == null) {
					defaultValues = new Dictionary<Type, object>();
				}
				if(!defaultValues.TryGetValue(memberInfo.MemberType, out result)) {
					result = Activator.CreateInstance(memberInfo.MemberType);
					defaultValues[memberInfo.MemberType] = result;
				}
			}
			return result;
		}
		private object GetCustomPropertyValue(IMemberInfo memberInfo) {
			object theValue = null;
			if(customPropertyStore != null) {
				customPropertyStore.TryGetValue(memberInfo, out theValue);
			}
			if(theValue == null) {
				theValue = CreateDefValue(memberInfo);
				if(theValue != null) {
					CustomPropertyStore[memberInfo] = theValue;
				}
			}
			return theValue;
		}
		object ICustomPropertyStore.GetCustomPropertyValue(IMemberInfo memberInfo) {
			return GetCustomPropertyValue(memberInfo);
		}
		bool ICustomPropertyStore.SetCustomPropertyValue(IMemberInfo memberInfo, object value) {
			object oldValue = GetCustomPropertyValue(memberInfo);
			if(CanSkipAssignment(oldValue, value)) {
				return false;
			}
			if(ReferenceEquals(value, null)) {
				CustomPropertyStore.Remove(memberInfo);
			}
			else {
				CustomPropertyStore[memberInfo] = value;
			}
			OnPropertyChanged(memberInfo.Name);
			return true;
		}
		private bool CanSkipAssignment(object oldValue, object newValue) {
			if(ReferenceEquals(oldValue, newValue))
				return true;
			else if(oldValue is ValueType && newValue is ValueType && Equals(oldValue, newValue))
				return true;
			else if(oldValue is string && newValue is string && Equals(oldValue, newValue))
				return true;
			else
				return false;
		}
		public void SetMemberValue(string propertyName, object newValue) {
			typeInfo.Value.FindMember(propertyName).SetValue(this, newValue);
		}
		public object GetMemberValue(string propertyName) {
			return typeInfo.Value.FindMember(propertyName).GetValue(this);
		}
		protected Boolean UpdateCalculatedPropertiesOnChanged {
			get { return updateCalculatedPropertiesOnChanged; }
			set { updateCalculatedPropertiesOnChanged = value; }
		}
		Boolean ICustomPropertyStore.UpdateCalculatedPropertiesOnChanged {
			get { return UpdateCalculatedPropertiesOnChanged; }
			set { UpdateCalculatedPropertiesOnChanged = value; }
		}
		#endregion
	}
	[DomainComponent]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public abstract class NonPersistentObjectImpl : NonPersistentEntityObject, IObjectSpaceLink {
		private IObjectSpace objectSpace;
		protected override ITypeInfo GetTypeInfo() {
			return (ObjectSpace != null) ? ObjectSpace.TypesInfo.FindTypeInfo(GetType()) : base.GetTypeInfo();
		}
		#region IObjectSpaceLink
		protected virtual void OnObjectSpaceChanging() {
		}
		protected virtual void OnObjectSpaceChanged() {
		}
		protected IObjectSpace ObjectSpace {
			get { return objectSpace; }
			set {
				if(objectSpace != value) {
					OnObjectSpaceChanging();
					objectSpace = value;
					OnObjectSpaceChanged();
				}
			}
		}
		IObjectSpace IObjectSpaceLink.ObjectSpace {
			get { return ObjectSpace; }
			set { ObjectSpace = value; }
		}
		#endregion
	}
	[DomainComponent]
	public abstract class NonPersistentBaseObject : NonPersistentObjectImpl {
		private Guid oid;
		public NonPersistentBaseObject() {
			this.oid = Guid.NewGuid();
		}
		public NonPersistentBaseObject(Guid oid) {
			this.oid = oid;
		}
		[Key]
		[VisibleInListView(false), VisibleInDetailView(false), VisibleInLookupListView(false)]
		public Guid Oid {
			get { return oid; }
		}
	}
	[DomainComponent]
	public abstract class NonPersistentLiteObject : NonPersistentEntityObject {
		private Guid oid;
		public NonPersistentLiteObject() {
			this.oid = Guid.NewGuid();
		}
		public NonPersistentLiteObject(Guid oid) {
			this.oid = oid;
		}
		[Key]
		[VisibleInListView(false), VisibleInDetailView(false), VisibleInLookupListView(false)]
		public Guid Oid {
			get { return oid; }
		}
	}
}
#endif