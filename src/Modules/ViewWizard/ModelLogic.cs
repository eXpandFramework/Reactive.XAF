using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.ViewWizard{
	public interface IModelReactiveModulesViewWizard : IModelReactiveModule{
		IModelViewWizard ViewWizard{ get; }
	}

    public interface IModelViewWizard:IModelNode{
        IModelWizardViews WizardViews{ get; } 
    }

    public interface IModelWizardViews:IModelList<IModelWizardView>,IModelNode{
        
    }

    public interface IModelWizardView:IModelNode{
        [CriteriaOptions("DetailView.ModelClass.TypeInfo")]
        [Editor("DevExpress.ExpressApp.Win.Core.ModelEditor.CriteriaModelEditorControl, DevExpress.ExpressApp.Win" + XafAssemblyInfo.VersionSuffix + XafAssemblyInfo.AssemblyNamePostfix, "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [Required]
        string Criteria{ get; set; }
        
        [Required]
        [DataSourceProperty(nameof(DetailViews))]
        IModelDetailView DetailView{ get; set; }
        [Browsable(false)]
        IModelList<IModelDetailView> DetailViews{ get; }
        IModelViewWizardChilds Childs{ get; }
    }

    [DomainLogic(typeof(IModelWizardView))]
    public class ModelWizardViewLogic{
        public static IModelList<IModelDetailView> Get_DetailViews(IModelWizardView modelWizardView) 
            => modelWizardView.Application.Views.OfType<IModelDetailView>().ToCalculatedModelNodeList();
    }
    public interface IModelViewWizardChilds:IModelList<IModelViewWizardChildItem>,IModelNode{
    }

    public interface IModelViewWizardChildItem:IModelNode{
        [DataSourceProperty(nameof(ChildDetailViews))]
        IModelDetailView ChildDetailView{ get; set; }
        [Browsable(false)]
        IModelList<IModelDetailView> ChildDetailViews{ get; }
    }

    [DomainLogic(typeof(IModelViewWizardChildItem))]
    public class ModelViewWizardChildItemLogic{
        public IModelList<IModelDetailView> Get_ChildDetailViews(IModelViewWizardChildItem item){
            var wizardObjectType = item.GetParent<IModelWizardView>().DetailView.ModelClass.TypeInfo.Type;
            return item.Application.Views.OfType<IModelDetailView>()
                .Where(view => view.ModelClass.TypeInfo.Type == wizardObjectType).ToCalculatedModelNodeList();
        }

    }
}
