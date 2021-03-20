using System;
using System.Collections.Generic;
using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using Xpand.Extensions.XAF.Attributes;

namespace Xpand.Extensions.XAF.NonPersistentObjects {

    [XafDefaultProperty(nameof(Caption))]
    [DomainComponent]
    public class ObjectString:NonPersistentBaseObject ,ICheckedListBoxItemsProvider {
        
        
        public event EventHandler<CheckListboxItemsProviderArgs> CheckedListBoxItems;

        public ObjectString(string name) {
            Name = name;
            Caption = name;
            
        }

        public ObjectString() {
            _c = this;
        }
        [Browsable(false)]
        public object Owner { get; set; }
        ObjectString _c;
        [DataSourceProperty(nameof(DataSource))]
        public ObjectString C {
            get => _c;
            set => SetPropertyValue(ref _c, value);
        }

        string _name;
        [DevExpress.ExpressApp.Data.Key]
        // [InvisibleInAllViews]
        
        [VisibleInAllViews]
        public string Name {
            get => _name;
            set => SetPropertyValue(ref _name, value);
        }

        string _caption;
        

        public string Caption {
            get => _caption;
            set => SetPropertyValue(ref _caption, value);
        }

        
        public static implicit operator string(ObjectString objectString) {
            return objectString?.Name;
        }

        public Dictionary<object, string> GetCheckedListBoxItems(string targetMemberName) {
            var args = new CheckListboxItemsProviderArgs(targetMemberName);
            OnCheckedListBoxItems(args);
            return args.Objects;
        }

        public event EventHandler ItemsChanged;

        protected virtual void OnItemsChanged() => ItemsChanged?.Invoke(this, EventArgs.Empty);

        protected virtual void OnCheckedListBoxItems(CheckListboxItemsProviderArgs e) => CheckedListBoxItems?.Invoke(this, e);

        [Browsable(false)] public IList<ObjectString> DataSource { get; } = new List<ObjectString>();


        // [Browsable(false)] public Type DataSourceType { get; } = typeof(ObjectString);

        // public void SetDatasource(IList objectStrings) => DataSource=objectStrings;
    }

    public class CheckListboxItemsProviderArgs:EventArgs {
        public string TargetMemberName { get; }

        public CheckListboxItemsProviderArgs(string targetMemberName) {
            TargetMemberName = targetMemberName;
            Objects = new Dictionary<object, string>();
        }

        public Dictionary<object, string> Objects { get;  }
    }



}