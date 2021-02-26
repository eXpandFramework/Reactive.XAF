 using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire{
	public interface IModelReactiveModulesJobScheduler : IModelReactiveModule{
		IModelJobScheduler JobScheduler{ get; }
	}

    public interface IModelJobScheduler:IModelNode{
        IModelJobSchedulerSources Sources{ get; } 
    }

    [ModelNodesGenerator(typeof(ModelJobSchedulerSourceModelGenerator))]
    public interface IModelJobSchedulerSources:IModelList<IModelJobSchedulerSource>,IModelNode{ }

    public class ModelJobSchedulerSourceModelGenerator:ModelNodesGeneratorBase {
        protected override void GenerateNodesCore(ModelNode node) { }
    }

    [KeyProperty(nameof(AssemblyName))]
    public interface IModelJobSchedulerSource:IModelNode{
        string AssemblyName { get; set; }
    }



}
