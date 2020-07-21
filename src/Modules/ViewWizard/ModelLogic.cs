using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.DC;
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
    // public interface IModelViewWizardItems:IModelList<IModelViewWizardItem>,IModelNode{
    //     
    // }
    // public interface IModelViewWizardItem{
    //     [DataSourceProperty("Application.Views")][Required]
    //     IModelView HostView{ get; set; }
    //     [DataSourceProperty("Parent."+nameof(IModelViewWizard.WizardViews))]
    //     [Required]
    //     IModelWizardView WizardView{ get; set; }
    //     
    // }

    public interface IModelWizardView:IModelNode{
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
