using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.ViewWizard{
	public interface IModelReactiveModulesViewWizard : IModelReactiveModule{
		IModelViewWizard ViewWizard{ get; }
	}

    public interface IModelViewWizard:IModelNode{
        IModelWizardViews Items{ get; }
        IModelWizardViews WizardViews{ get; } 
    }

    public interface IModelWizardViews:IModelList<IModelWizardView>,IModelNode{
        
    }
    public interface IModelViewWizardItems:IModelList<IModelViewWizardItem>,IModelNode{
        
    }
    public interface IModelViewWizardItem{
        [DataSourceProperty("Application.Views")][Required]
        IModelView HostView{ get; set; }
        [DataSourceProperty("Parent."+nameof(IModelViewWizard.WizardViews))]
        [Required]
        IModelWizardView WizardView{ get; set; }
        
    }

    public interface IModelWizardView:IModelNode{
        [Required]
        IModelDetailView DetailView{ get; set; }
        IModelViewWizardChilds Childs{ get; }
    }

    public interface IModelViewWizardChilds:IModelList<IModelDetailView>,IModelNode{
    }

    
}
