using System.ComponentModel;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.XAF.Modules.Reactive.Logger.Hub{
    public interface IModelServerPort : IModelNode{
        [Required]
        [DefaultValue("localhost")]
        string Host{ get; set; }
        int Port{ get; set; }
    }

    [ModelNodesGenerator(typeof(ModelServerPosrtsGenerator))]
    public interface IModelServerPortsList:IModelNode,IModelList<IModelServerPort>{
        
    }

    public class ModelServerPosrtsGenerator:ModelNodesGeneratorBase{
        protected override void GenerateNodesCore(ModelNode node){
            var modelServerPort = node.AddNode<IModelServerPort>("default");
            modelServerPort.Port = 61456;
        }
    }

    public interface IModelServerPorts:IModelReactiveModule{
        IModelServerPortsList Ports{ get; }
    }

    
}