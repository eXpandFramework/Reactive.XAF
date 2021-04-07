using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Blazor.Model{
	public interface IModelReactiveModulesBlazor : IModelReactiveModule{
		IModelBlazor Blazor{ get; }
	}

    public interface IModelBlazor:IModelNode{
        IModelListViewFeatures ListViewFeatures{ get; } 
    }

    public interface IModelListViewFeatures:IModelList<IModelListViewFeature>,IModelNode{
    }


    [ModelAbstractClass]
    public interface IModelListViewFeature:IModelNode{ }

    
    
    
}
