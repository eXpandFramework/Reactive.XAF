using System;
using System.ComponentModel;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.XAF.ModelExtensions;

namespace Xpand.XAF.Modules.Office.Cloud.Google.Tasks{
    
    public interface IModelGoogleTasks:IModelNode{
        IModelTasks Tasks{ get; }
    }

    [PublicAPI]
    public interface IModelTasks:IModelNode{
        [DefaultValue(GoogleTasksService.DefaultTasksListId)]
        [Required]
        string DefaultTaskListName{ get; set; }
        IModelTasksItems Items{ get; }
    }

    [DomainLogic(typeof(IModelTasks))]
    public static class ModelTasksLogic{
        
        public static IObservable<IModelTasks> Tasks(this IObservable<IModelGoogle> source) => source.Select(modules => modules.Tasks());

        public static IModelTasks Tasks(this IModelGoogle modelGoogle) => ((IModelGoogleTasks) modelGoogle).Tasks;

        public static IModelTasks Tasks(this IModelOfficeGoogle reactiveModules) => reactiveModules.Google.Tasks();
    }

    public interface IModelTasksItems : IModelList<IModelTasksItem>,IModelNode{
    }

    public interface IModelTasksItem:IModelSynchronizationType,IModelObjectViewDependency{
    }
}
