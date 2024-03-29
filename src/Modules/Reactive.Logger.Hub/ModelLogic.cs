﻿using System.ComponentModel;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.XAF.Modules.Reactive.Logger.Hub{
    public interface IModelLoggerClientRange:IModelLoggerPort{
        [DefaultValue(61497)]
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
        [DefaultValue(true)]
        bool Enabled{ get; set; }
        [Required]
        [DefaultValue("localhost")]
        string Host{ get; set; }
        [DefaultValue(61456)]
        int Port{ get; set; }
    }

    [ModelNodesGenerator(typeof(ModelServerPortGenerator))]
    public interface IModelLoggerPortsList:IModelNode,IModelList<IModelLoggerPort>{
        [DefaultValue(true)]
        bool Enabled{ get; set; }
    }

    public class ModelServerPortGenerator:ModelNodesGeneratorBase{
        protected override void GenerateNodesCore(ModelNode node){
            node.AddNode<IModelLoggerClientRange>("default client range 61456-61496");
            node.AddNode<IModelLoggerServerPort>("default server port 61456");
        }
    }

    public interface IModelReactiveLoggerHub:IModelReactiveModule{
        IModelLoggerPortsList LoggerPorts{ get; }
        
    }

    
}