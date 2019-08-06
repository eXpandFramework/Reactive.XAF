using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.ModelViewInheritance.Tests.BOModel{
    [DefaultClassOptions]
    [CloneModelView(CloneViewType.ListView, ListViewBase)]
    [CloneModelView(CloneViewType.ListView, ListViewBaseNested)]
    public class Element : CustomBaseObject{
        public const string ListViewBase = "Element_ListViewBase";
        public const string ListViewBaseNested = "Element_ListViewNestedBase";
        private string _name;


        private string _street;

        public Element(Session session)
            : base(session){
        }

        public string Name{
            get => _name;
            set => SetPropertyValue("Name", ref _name, value);
        }

        public string Street{
            get => _street;
            set => SetPropertyValue("Street", ref _street, value);
        }
    }
}