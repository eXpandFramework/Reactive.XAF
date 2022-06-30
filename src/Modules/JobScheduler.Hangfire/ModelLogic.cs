 using System.ComponentModel;
 using DevExpress.Blazor.Internal;
 using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
 using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire{
	public interface IModelReactiveModulesJobScheduler : IModelReactiveModule{
		IModelJobScheduler JobScheduler{ get; }
	}

    public interface IModelJobScheduler:IModelNode{
        IModelJobSchedulerSources Sources{ get; } 
        IModelJobActions Actions { get; }
    }

    [ModelNodesGenerator(typeof(ModelJobActionsNodesGenerator))]
    public interface IModelJobActions : IModelList<IModelJobAction>, IModelNode {

    }

    [KeyProperty(nameof(ActionId))]
    public interface IModelJobAction:IModelNode {
        [Browsable(false)]
        string ActionId { get; set; }
        [Required]
        IModelAction Action { get; set; }
        [DefaultValue(true)]
        bool Enable { get; set; }
    }

    public class ModelJobActionLogic {
        public static IModelAction Get_Action(IModelJobAction action) 
            => action.Application.ActionDesign.Actions[action.ActionId];

        public void Set_Action(IModelJobAction jobAction,IModelAction action) 
            => jobAction.ActionId = action.Id;
    }

    public class ModelJobActionsNodesGenerator:ModelNodesGeneratorBase {
        protected override void GenerateNodesCore(ModelNode node)
        => ((IModelNode)node).Application.ActionDesign.Actions
            .ForEach(action => node.AddNode<IModelJobAction>(action.Id).Action=action);
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
