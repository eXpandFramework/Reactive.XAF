#if XAF192

using DevExpress.Data.Filtering;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using System;
using System.Collections;
using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using DevExpress.ExpressApp.DC;

namespace DevExpress.ExpressApp
{
      public abstract class DynamicCollectionBase : 
    ICancelAddNew,
    IBindingList,
    ICollection,
    IEnumerable,
    IList,
    ITypedList,
    IDisposable
  {
    protected IObjectSpace objectSpace;
    protected Type objectType;
    protected CriteriaOperator criteria;
    protected List<SortProperty> sorting;
    private bool inTransaction;
    protected int topReturnedObjectsCount;
    private bool deleteObjectOnRemove;
    private List<object> objects;
    protected XafPropertyDescriptorCollection propertyDescriptorCollection;
    private bool allowNew;
    private bool allowEdit;
    private bool allowRemove;
    private object newObject;
    private int newObjectIndex;
    private bool isDisposed;

    private void ClearObjects()
    {
      if (this.objects != null)
        this.objects.Clear();
      this.objects = (List<object>) null;
    }

    private void RemoveObject(object obj, int index)
    {
      this.objects.RemoveAt(index);
      this.RaiseListChangedEvent(new ListChangedEventArgs(ListChangedType.ItemDeleted, index));
      if (!this.deleteObjectOnRemove)
        return;
      this.objectSpace.Delete(obj);
    }

    private void ObjectSpace_ObjectReloaded(object sender, ObjectManipulatingEventArgs e)
    {
      if (this.objects == null || e.Object == null || !this.objectType.IsAssignableFrom(e.Object.GetType()))
        return;
      int newIndex = this.objects.IndexOf(e.Object);
      if (newIndex < 0)
        return;
      this.RaiseListChangedEvent(new ListChangedEventArgs(ListChangedType.ItemChanged, newIndex, -1));
    }

    /// <summary>
    ///   <para>Occurs after the collection was populated with objects.</para>
    /// </summary>
    public event EventHandler Loaded;

    private void RaiseLoaded()
    {
      EventHandler loaded = this.Loaded;
      if (loaded == null)
        return;
      loaded((object) this, EventArgs.Empty);
    }

    private void InitObjects()
    {
      if (this.objects != null)
        return;
      this.objects = new List<object>();
      try
      {
        foreach (object obj in this.GetObjects())
        {
          if (!this.objectSpace.IsObjectToDelete(obj))
            this.objects.Add(obj);
        }
        this.RaiseLoaded();
      }
      catch (Exception ex)
      {
        throw this.WrapException(ex);
      }
    }

    protected abstract IEnumerable GetObjects();

    protected virtual Exception WrapException(Exception exception) => exception;

    protected internal void RaiseListChangedEvent(ListChangedEventArgs eventArgs)
    {
      ListChangedEventHandler listChanged = this.ListChanged;
      if (listChanged == null)
        return;
      listChanged((object) this, eventArgs);
    }

    protected virtual IList<IMemberInfo> GetDefaultDisplayableMembers(
      ITypeInfo typeInfo)
    {
      List<IMemberInfo> memberInfoList = new List<IMemberInfo>();
      foreach (IMemberInfo member in typeInfo.Members)
      {
        if (member.IsVisible || member == member.Owner.KeyMember)
          memberInfoList.Add(member);
      }
      return (IList<IMemberInfo>) memberInfoList;
    }

    protected List<object> Objects
    {
      get
      {
        this.InitObjects();
        return this.objects;
      }
    }

    /// <summary>
    ///   <para>Initializes a new instance of the <see cref="T:DevExpress.ExpressApp.DynamicCollectionBase" /> class with specified settings.</para>
    /// </summary>
    /// <param name="objectSpace">An Object Space for processing the collection objects.</param>
    /// <param name="objectType">A type of objects in the collection.</param>
    /// <param name="criteria">The filter criteria. The collection contains only objects that match this criteria.</param>
    /// <param name="sorting">A list of <see cref="T:DevExpress.Xpo.SortProperty" /> objects that specify the sort order for the collection.</param>
    /// <param name="inTransaction">true if the specified criteria and sorting parameters are applied to all objects (in the database and retrieved); otherwise, false.</param>
    public DynamicCollectionBase(
      IObjectSpace objectSpace,
      Type objectType,
      CriteriaOperator criteria,
      IList<SortProperty> sorting,
      bool inTransaction)
    {
      this.objectSpace = objectSpace;
      this.objectType = objectType;
      this.criteria = criteria;
      this.sorting = new List<SortProperty>();
      if (sorting != null)
        this.sorting.AddRange((IEnumerable<SortProperty>) sorting);
      this.inTransaction = inTransaction;
      this.propertyDescriptorCollection = new XafPropertyDescriptorCollection(objectSpace.TypesInfo.FindTypeInfo(objectType));
      foreach (IMemberInfo displayableMember in (IEnumerable<IMemberInfo>) this.GetDefaultDisplayableMembers(this.propertyDescriptorCollection.TypeInfo))
        this.propertyDescriptorCollection.CreatePropertyDescriptor(displayableMember, displayableMember.Name);
      this.newObjectIndex = -1;
      this.allowNew = true;
      this.allowEdit = true;
      this.allowRemove = true;
      objectSpace.ObjectReloaded += new EventHandler<ObjectManipulatingEventArgs>(this.ObjectSpace_ObjectReloaded);
    }

    /// <summary>
    ///   <para>Disposes of all resources this <see cref="T:DevExpress.ExpressApp.DynamicCollectionBase" /> object uses. This method is implemented to support the <see cref="T:System.IDisposable" /> interface.</para>
    /// </summary>
    public void Dispose()
    {
      this.isDisposed = true;
      this.ListChanged = (ListChangedEventHandler) null;
      if (this.objects != null)
      {
        this.objects.Clear();
        this.objects = (List<object>) null;
      }
      if (this.objectSpace != null)
      {
        this.objectSpace.ObjectReloaded -= new EventHandler<ObjectManipulatingEventArgs>(this.ObjectSpace_ObjectReloaded);
        this.objectSpace = (IObjectSpace) null;
      }
      this.propertyDescriptorCollection = (XafPropertyDescriptorCollection) null;
    }

    /// <summary>
    ///   <para>Clears the collection. The <see cref="E:DevExpress.ExpressApp.DynamicCollection.FetchObjects" /> event will occur on the next access to this collection.</para>
    /// </summary>
    public void Reload()
    {
      if (!this.IsLoaded)
        return;
      this.ClearObjects();
      this.RaiseListChangedEvent(new ListChangedEventArgs(ListChangedType.Reset, 0));
    }

    /// <summary>
    ///   <para>Returns the Object Space used to manipulate the collection's objects.</para>
    /// </summary>
    /// <value>An Object Space used to manipulate the collection's objects.</value>
    public IObjectSpace ObjectSpace => this.objectSpace;

    /// <summary>
    ///   <para>Returns the type of objects that the <see cref="P:DevExpress.ExpressApp.FetchObjectsEventArgs.Objects" /> collection can contain.</para>
    /// </summary>
    /// <value>A type of objects that the <see cref="P:DevExpress.ExpressApp.FetchObjectsEventArgs.Objects" /> collection can contain.</value>
    public Type ObjectType => this.objectType;

    /// <summary>
    ///   <para>Specifies criteria used to filter objects in the collection.</para>
    /// </summary>
    /// <value>A criteria used to filter objects in the collection.</value>
    public CriteriaOperator Criteria
    {
      get => this.criteria;
      set
      {
        if ((object) this.criteria == (object) value)
          return;
        this.criteria = value;
        this.Reload();
      }
    }

    /// <summary>
    ///   <para>Specifies the list of <see cref="T:DevExpress.Xpo.SortProperty" /> objects that specify the sort order for the collection.</para>
    /// </summary>
    /// <value>A list of <see cref="T:DevExpress.Xpo.SortProperty" /> objects that specify the sort order for the collection.</value>
    public IList<SortProperty> Sorting
    {
      get => (IList<SortProperty>) this.sorting.AsReadOnly();
      set
      {
        this.sorting.Clear();
        if (value != null)
          this.sorting.AddRange((IEnumerable<SortProperty>) value);
        this.Reload();
      }
    }

    /// <summary>
    ///   <para>Specifies if the specified <see cref="P:DevExpress.ExpressApp.DynamicCollectionBase.Criteria" /> and <see cref="P:DevExpress.ExpressApp.DynamicCollectionBase.Sorting" /> parameters are applied to all objects (in the database and retrieved).</para>
    /// </summary>
    /// <value>true if the specified <see cref="P:DevExpress.ExpressApp.DynamicCollectionBase.Criteria" /> and <see cref="P:DevExpress.ExpressApp.DynamicCollectionBase.Sorting" /> parameters are applied to all objects (in the database and retrieved); otherwise, false.</value>
    public bool InTransaction => this.inTransaction;

    /// <summary>
    ///   <para>Specifies the maximum number of objects that can be retrieved from the collection.</para>
    /// </summary>
    /// <value>The maximum number of objects that can be retrieved from the collection.</value>
    public int TopReturnedObjectsCount
    {
      get => this.topReturnedObjectsCount;
      set => this.topReturnedObjectsCount = value;
    }

    /// <summary>
    ///   <para>For internal use.</para>
    /// </summary>
    /// <value></value>
    public string DisplayableProperties
    {
      get => this.propertyDescriptorCollection.DisplayableMembers;
      set
      {
        if (!(this.propertyDescriptorCollection.DisplayableMembers != value))
          return;
        this.propertyDescriptorCollection.DisplayableMembers = value;
        this.RaiseListChangedEvent(new ListChangedEventArgs(ListChangedType.PropertyDescriptorChanged, -1, -1));
      }
    }

    /// <summary>
    ///   <para>Specifies whether objects are deleted from a storage when they are removed from the collection.</para>
    /// </summary>
    /// <value>true if objects are deleted from a storage when they are removed from the collection; otherwise, false.</value>
    public bool DeleteObjectOnRemove
    {
      get => this.deleteObjectOnRemove;
      set => this.deleteObjectOnRemove = value;
    }

    /// <summary>
    ///   <para>Indicates whether the collection was populated with objects.</para>
    /// </summary>
    /// <value>true if the collection was populated with objects; otherwise, false.</value>
    public bool IsLoaded => this.objects != null;

    void ICancelAddNew.CancelNew(int itemIndex)
    {
      if (this.newObject == null || this.newObjectIndex != itemIndex)
        return;
      this.objects.Remove(this.newObject);
      this.objectSpace.RemoveFromModifiedObjects(this.newObject);
      this.newObject = (object) null;
      this.RaiseListChangedEvent(new ListChangedEventArgs(ListChangedType.ItemDeleted, this.newObjectIndex));
      this.newObjectIndex = -1;
    }

    void ICancelAddNew.EndNew(int itemIndex)
    {
      if (this.newObject == null || this.newObjectIndex != itemIndex)
        return;
      this.objectSpace.SetModified(this.newObject);
      this.newObject = (object) null;
      this.newObjectIndex = -1;
    }

    void IBindingList.AddIndex(PropertyDescriptor property)
    {
    }

    object IBindingList.AddNew()
    {
      if (!this.allowNew)
        throw new Exception("AddNew is not allowed.");
      this.InitObjects();
      this.newObject = this.objectSpace.CreateObject(this.objectType);
      this.objects.Add(this.newObject);
      this.RaiseListChangedEvent(new ListChangedEventArgs(ListChangedType.ItemAdded, this.objects.Count - 1));
      this.newObjectIndex = this.objects.Count - 1;
      return this.newObject;
    }

    void IBindingList.ApplySort(
      PropertyDescriptor memberDescriptor,
      ListSortDirection direction)
    {
      this.sorting.Clear();
      this.sorting.Add(new SortProperty(memberDescriptor.Name, direction == ListSortDirection.Ascending ? SortingDirection.Ascending : SortingDirection.Descending));
      this.Reload();
    }

    void IBindingList.RemoveSort()
    {
      if (this.sorting.Count <= 0)
        return;
      this.sorting.Clear();
      this.Reload();
    }

    void IBindingList.RemoveIndex(PropertyDescriptor property)
    {
    }

    int IBindingList.Find(PropertyDescriptor property, object key)
    {
      this.InitObjects();
      for (int index = 0; index < this.objects.Count; ++index)
      {
        object obj = property.GetValue(this.objects[index]);
        if (obj != null)
        {
          if (obj.Equals(key))
            return index;
        }
        else if (key == null)
          return index;
      }
      return -1;
    }

    /// <summary>
    ///   <para>Specifies whether new objects can be added to the collection. This property is implemented to support the <see cref="T:System.ComponentModel.IBindingList" /> interface.</para>
    /// </summary>
    /// <value>true if new objects can be added to the collection; otherwise, false.</value>
    public bool AllowNew
    {
      get => this.allowNew;
      set => this.allowNew = value;
    }

    /// <summary>
    ///   <para>Specifies whether the collection is read-only. This property is implemented to support the <see cref="T:System.ComponentModel.IBindingList" /> interface.</para>
    /// </summary>
    /// <value>false if the collection is read-only; otherwise, true.</value>
    public bool AllowEdit
    {
      get => this.allowEdit;
      set => this.allowEdit = value;
    }

    /// <summary>
    ///   <para>Specifies whether objects can be removed from the collection. This property is implemented to support the <see cref="T:System.ComponentModel.IBindingList" /> interface.</para>
    /// </summary>
    /// <value>true if objects can be removed from the collection; otherwise, false.</value>
    public bool AllowRemove
    {
      get => this.allowRemove;
      set => this.allowRemove = value;
    }

    bool IBindingList.IsSorted => this.sorting.Count > 0;

    bool IBindingList.SupportsSorting => true;

    PropertyDescriptor IBindingList.SortProperty => this.sorting.Count > 0 ? ((ITypedList) this).GetItemProperties((PropertyDescriptor[]) null).Find(this.sorting[0].PropertyName, false) : (PropertyDescriptor) null;

    ListSortDirection IBindingList.SortDirection => this.sorting.Count > 0 && this.sorting[0].Direction == SortingDirection.Descending ? ListSortDirection.Descending : ListSortDirection.Ascending;

    bool IBindingList.SupportsSearching => true;

    bool IBindingList.SupportsChangeNotification => true;

    /// <summary>
    ///   <para>Occurs when the collection contents are changed. This event is implemented to support the <see cref="T:System.ComponentModel.IBindingList" /> interface.</para>
    /// </summary>
    public event ListChangedEventHandler ListChanged;

    /// <summary>
    ///   <para>Adds the specified object to the collection. This method is implemented to support the <see cref="T:System.Collections.IList" /> interface.</para>
    /// </summary>
    /// <param name="obj">An object to be added to the collection.</param>
    /// <returns>An index of the added object. -1 if the object was not added to the collection.</returns>
    public int Add(object obj)
    {
      this.InitObjects();
      int newIndex = this.objects.IndexOf(obj);
      if (newIndex < 0)
      {
        this.objects.Add(obj);
        newIndex = this.objects.Count - 1;
        this.RaiseListChangedEvent(new ListChangedEventArgs(ListChangedType.ItemAdded, newIndex));
      }
      return newIndex;
    }

    /// <summary>
    ///   <para>Inserts the specified object into the collection at the specified position. This method is implemented to support the <see cref="T:System.Collections.IList" /> interface.</para>
    /// </summary>
    /// <param name="index">An index where this method inserts the specified object.</param>
    /// <param name="obj">An object to insert into the collection.</param>
    public void Insert(int index, object obj)
    {
      this.InitObjects();
      int num = this.objects.IndexOf(obj);
      if (num == index || num + 1 == index)
        return;
      this.objects.Insert(index, obj);
      if (num < 0)
        this.RaiseListChangedEvent(new ListChangedEventArgs(ListChangedType.ItemAdded, index));
      else if (num < index)
      {
        this.objects.RemoveAt(num);
        this.RaiseListChangedEvent(new ListChangedEventArgs(ListChangedType.ItemMoved, index - 1, num));
      }
      else
      {
        this.objects.RemoveAt(num + 1);
        this.RaiseListChangedEvent(new ListChangedEventArgs(ListChangedType.ItemMoved, index, num));
      }
    }

    private void CheckAllowRemove()
    {
      if (!this.allowRemove)
        throw new Exception("Remove is not allowed.");
    }

    /// <summary>
    ///   <para>Removes the specified object from the collection. This method is implemented to support the <see cref="T:System.Collections.IList" /> interface.</para>
    /// </summary>
    /// <param name="obj">An object to remove.</param>
    public void Remove(object obj)
    {
      this.CheckAllowRemove();
      this.InitObjects();
      int index = this.objects.IndexOf(obj);
      if (index < 0)
        return;
      this.RemoveObject(obj, index);
    }

    /// <summary>
    ///   <para>Removes an object from the specified index in the collection. This method is implemented to support the <see cref="T:System.Collections.IList" /> interface.</para>
    /// </summary>
    /// <param name="index">An index of an object to be removed from the collection.</param>
    public void RemoveAt(int index)
    {
      this.CheckAllowRemove();
      this.InitObjects();
      if (index < 0 || index >= this.objects.Count)
        return;
      this.RemoveObject(this.objects[index], index);
    }

    /// <summary>
    ///   <para>Removes all objects from the collection. This method is implemented to support the <see cref="T:System.Collections.IList" /> interface.</para>
    /// </summary>
    public void Clear()
    {
      this.CheckAllowRemove();
      this.Reload();
    }

    /// <summary>
    ///   <para>Checks whether the collection contains the specified object. This method is implemented to support the <see cref="T:System.Collections.IList" /> interface.</para>
    /// </summary>
    /// <param name="obj">An object this method checks.</param>
    /// <returns>true if the collection contains the specified object; otherwise, false.</returns>
    public bool Contains(object obj)
    {
      this.InitObjects();
      return this.objects.Contains(obj);
    }

    /// <summary>
    ///   <para>Determines the index of the specified object in the collection. This method is implemented to support the <see cref="T:System.Collections.IList" /> interface.</para>
    /// </summary>
    /// <param name="obj">An object whom index this method determines.</param>
    /// <returns>An index of the specified object. -1 if the collection does not contain this object.</returns>
    public int IndexOf(object obj)
    {
      this.InitObjects();
      return this.objects.IndexOf(obj);
    }

    /// <summary>
    ///   <para>Specifies whether the collection is read-only. This property is implemented to support the <see cref="T:System.Collections.IList" /> interface.</para>
    /// </summary>
    /// <value>true if the collection is read-only; otherwise, false.</value>
    public bool IsReadOnly => false;

    /// <summary>
    ///   <para>Indicates whether the collection has a fixed size. This property is implemented to support the <see cref="T:System.Collections.IList" /> interface.</para>
    /// </summary>
    /// <value>true if the collection has a fixed size; otherwise, false.</value>
    public bool IsFixedSize => false;

    /// <summary>
    ///   <para>Gets or sets an object at the specified index. This property is implemented to support the <see cref="T:System.Collections.IList" /> interface.</para>
    /// </summary>
    /// <param name="index">An index of the object to be returned.</param>
    /// <value>An object at the specified index.</value>
    public object this[int index]
    {
      get
      {
        this.InitObjects();
        return index >= 0 && index < this.objects.Count ? this.objects[index] : (object) null;
      }
      set => throw new Exception("List is read only");
    }

    /// <summary>
    ///   <para>Copies the collection to the specified array. This method is implemented to support the <see cref="T:System.Collections.ICollection" /> interface.</para>
    /// </summary>
    /// <param name="array">A target array.</param>
    /// <param name="index">The array's index that is the first position for collection objects.</param>
    public void CopyTo(Array array, int index)
    {
      this.InitObjects();
      ((ICollection) this.objects).CopyTo(array, index);
    }

    /// <summary>
    ///   <para>Returns the count of objects in the collection. This property is implemented to support the <see cref="T:System.Collections.ICollection" /> interface.</para>
    /// </summary>
    /// <value>The count of objects in the collection.</value>
    public int Count
    {
      get
      {
        if (this.isDisposed)
          return 0;
        this.InitObjects();
        return this.objects.Count;
      }
    }

    /// <summary>
    ///   <para>Specifies whether the collection has been disposed of. For internal use.</para>
    /// </summary>
    /// <value>true, if the collection has been disposed of; otherwise, false.</value>
    public bool IsDisposed => this.isDisposed;

    /// <summary>
    ///   <para>Indicates whether access to the collection is synchronized (thread safe). This property is implemented to support the <see cref="T:System.Collections.ICollection" /> interface.</para>
    /// </summary>
    /// <value>true if access to the collection is synchronized (thread safe); otherwise, false.</value>
    public bool IsSynchronized => false;

    /// <summary>
    ///   <para>Gets an object that can be used to synchronize access to the collection. This property is implemented to support the <see cref="T:System.Collections.ICollection" /> interface.</para>
    /// </summary>
    /// <value>An object that can be used to synchronize access to the collection.</value>
    public object SyncRoot => (object) this;

    IEnumerator IEnumerable.GetEnumerator()
    {
      this.InitObjects();
      return (IEnumerator) this.objects.GetEnumerator();
    }

    PropertyDescriptorCollection ITypedList.GetItemProperties(
      PropertyDescriptor[] listAccessors)
    {
      if (listAccessors != null && listAccessors.Length != 0)
        throw new Exception("listAccessors != null");
      return (PropertyDescriptorCollection) this.propertyDescriptorCollection;
    }

    string ITypedList.GetListName(PropertyDescriptor[] listAccessors) => "";
  }

  /// <summary>
  ///   <para>A proxy collection that allows you to filter and sort an original collection without its change.</para>
  /// </summary>
  public class DynamicCollection : DynamicCollectionBase
  {
    private static IEnumerable emptyEnumerable = (IEnumerable) new object[0];

    /// <summary>
    ///   <para>Initializes a new instance of the <see cref="T:DevExpress.ExpressApp.DynamicCollection" /> class with specified settings.</para>
    /// </summary>
    /// <param name="objectSpace">An Object Space for processing collection objects.</param>
    /// <param name="objectType">A type of objects in the collection.</param>
    /// <param name="criteria">The filter criteria. The collection contains only objects that match this criteria.</param>
    /// <param name="sorting">A list of <see cref="T:DevExpress.Xpo.SortProperty" /> objects that specify the sort order for the collection.</param>
    /// <param name="inTransaction">true if the specified criteria and sorting parameters are applied to all objects (in the database and retrieved); otherwise, false.</param>
    public DynamicCollection(
      IObjectSpace objectSpace,
      Type objectType,
      CriteriaOperator criteria,
      IList<SortProperty> sorting,
      bool inTransaction)
      : base(objectSpace, objectType, criteria, sorting, inTransaction)
    {
    }

    /// <summary>
    ///   <para>Initializes a new instance of the <see cref="T:DevExpress.ExpressApp.DynamicCollection" /> class with specified settings.</para>
    /// </summary>
    /// <param name="objectSpace">An Object Space for processing the collection objects.</param>
    /// <param name="objectType">A type of objects in the collection.</param>
    public DynamicCollection(IObjectSpace objectSpace, Type objectType)
      : base(objectSpace, objectType, (CriteriaOperator) null, (IList<SortProperty>) null, false)
    {
    }

    /// <summary>
    ///   <para>Occurs when the DynamicCollection contents are accessed for the first time, when this collection is reloaded, or its <see cref="P:DevExpress.ExpressApp.DynamicCollectionBase.Criteria" /> and <see cref="P:DevExpress.ExpressApp.DynamicCollectionBase.Sorting" /> parameters are changed. Handle this event and set the <see cref="P:DevExpress.ExpressApp.FetchObjectsEventArgs.Objects" /> argument the DynamicCollection contents.</para>
    /// </summary>
    public event EventHandler<FetchObjectsEventArgs> FetchObjects;

    protected override IEnumerable GetObjects()
    {
      FetchObjectsEventArgs e = new FetchObjectsEventArgs(this.ObjectType, this.Criteria, this.Sorting, this.TopReturnedObjectsCount, this.InTransaction);
      EventHandler<FetchObjectsEventArgs> fetchObjects = this.FetchObjects;
      if (fetchObjects != null)
        fetchObjects((object) this, e);
      if (e.Objects == null)
        return DynamicCollection.emptyEnumerable;
      if (!e.ShapeData)
        return e.Objects;
      ExpressionEvaluator filterEvaluator = this.objectSpace.GetExpressionEvaluator(this.ObjectType, this.Criteria);
      IEnumerable<object> source = e.Objects.Cast<object>().Where<object>((Func<object, bool>) (o => filterEvaluator.Fit(o)));
      if (this.Sorting != null)
      {
        foreach (SortProperty sortProperty in (IEnumerable<SortProperty>) this.Sorting)
        {
          ExpressionEvaluator sortingEvaluator = this.objectSpace.GetExpressionEvaluator(this.ObjectType, sortProperty.Property);
          source = sortProperty.Direction != SortingDirection.Ascending ? (IEnumerable<object>) source.OrderByDescending<object, object>((Func<object, object>) (o => sortingEvaluator.Evaluate(o))) : (IEnumerable<object>) source.OrderBy<object, object>((Func<object, object>) (o => sortingEvaluator.Evaluate(o)));
        }
      }
      if (this.TopReturnedObjectsCount > 0)
        source = source.Take<object>(this.TopReturnedObjectsCount);
      return (IEnumerable) source;
    }
  }

  public class FetchObjectsEventArgs:EventArgs {
    public FetchObjectsEventArgs(Type objectType, CriteriaOperator criteria, IList<SortProperty> sorting, int topReturnedObjectsCount, bool inTransaction) {
      throw new NotImplementedException();
    }

    public IEnumerable Objects { get; }
    public bool ShapeData { get; set; }
  }
}

#endif