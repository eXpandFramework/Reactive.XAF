using System.ComponentModel;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.XAF.Modules.Reactive.Logger.Hub{
    public interface IModelLoggerClientRange:IModelLoggerPort{
        [DefaultValue(61486)]
        int EndPort{ get; set; }
        [Required]
        [DefaultValue("localhost")]
        string Host{ get; set; }
        [DefaultValue(61456)]
        int StartPort{ get; set; }
    }
    [ModelAbstractClass]
    public interface IModelLoggerPort : IModelNode{
    }

    public interface IModelLoggerServerPort : IModelLoggerPort{
        [Required]
        [DefaultValue("localhost")]
        string Host{ get; set; }
        [DefaultValue(61456)]
        int Port{ get; set; }
    }

    [ModelNodesGenerator(typeof(ModelServerPosrtsGenerator))]
    public interface IModelLoggerPortsList:IModelNode,IModelList<IModelLoggerPort>{
        
    }

    public class ModelServerPosrtsGenerator:ModelNodesGeneratorBase{
        protected override void GenerateNodesCore(ModelNode node){
            node.AddNode<IModelLoggerClientRange>("default client range 61456-61486");
            node.AddNode<IModelLoggerServerPort>("default server port 61456");
        }
    }

    public interface IModelServerPorts:IModelReactiveModule{
        IModelLoggerPortsList LoggerPorts{ get; }
    }

    
}