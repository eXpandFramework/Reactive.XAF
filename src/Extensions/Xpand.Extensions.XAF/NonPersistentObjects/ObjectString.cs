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

        public ObjectString() => _instance = this;

        [Browsable(false)]
        public object Owner { get; set; }
        ObjectString _instance;
        [DataSourceProperty(nameof(DataSource))]
        [Attributes.DisplayName("Name")][VisibleInListView(false)][VisibleInLookupListView(false)]
        public ObjectString Instance {
            get => _instance;
            set => SetPropertyValue(ref _instance, value);
        }

        string _name;
        // [DevExpress.ExpressApp.Data.Key]
        [InvisibleInAllViews]
        public string Name {
            get => _name;
            set => SetPropertyValue(ref _name, value);
        }

        string _caption;
        
        [VisibleInDetailView(false)]
        [Attributes.DisplayName("Name")]
        public string Caption {
            get => _caption;
            set => SetPropertyValue(ref _caption, value);
        }

        protected override void OnPropertyChanged(string memberName = "") {
            base.OnPropertyChanged(memberName);
            if (memberName == nameof(Instance)) {
                Caption = Instance?.Caption;
                Name = Instance?.Name;
            }
        }

        public static implicit operator string(ObjectString objectString) => objectString?.Name;

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

    public class CheckListboxItemsProviderArgs(string targetMemberName) : EventArgs {
        public string TargetMemberName { get; } = targetMemberName;

        public Dictionary<object, string> Objects { get;  } = new();
    }



}