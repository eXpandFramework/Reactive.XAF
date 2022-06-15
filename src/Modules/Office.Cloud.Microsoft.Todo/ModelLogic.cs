using System;
using System.ComponentModel;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;

using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.XAF.ModelExtensions.Shapes;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo{
    
    public interface IModelMicrosoftTodo:IModelNode{
        IModelTodo Todo{ get; }
    }

    
    public interface IModelTodo:IModelNode{
        [DefaultValue(MicrosoftTodoService.DefaultTodoListId)]
        [Required]
        string DefaultTodoListName{ get; set; }
        IModelTodoItems Items{ get; }
    }

    [DomainLogic(typeof(IModelTodo))]
    public static class ModelTodoLogic{
        
        public static IObservable<IModelTodo> Todo(this IObservable<IModelMicrosoft> source) => source.Select(modules => modules.Todo());

        public static IModelTodo Todo(this IModelMicrosoft modelMicrosoft) => ((IModelMicrosoftTodo) modelMicrosoft).Todo;

        public static IModelTodo Todo(this IModelOfficeMicrosoft reactiveModules) => reactiveModules.Microsoft.Todo();
    }

    public interface IModelTodoItems : IModelList<IModelTodoItem>,IModelNode{
    }

    public interface IModelTodoItem:IModelSynchronizationType,IModelObjectViewDependency{
    }
}
