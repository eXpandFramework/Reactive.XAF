using System;
using System.ComponentModel;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.ModelExtensions;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo{
    
    public interface IModelMicrosoftTodo:IModelNode{
        IModelTodo Todo{ get; }
    }

    [PublicAPI]
    public interface IModelTodo:IModelNode{
        [DefaultValue(TodoService.DefaultTodoListId)]
        [Required]
        string DefaultTodoListName{ get; set; }
        IModelTodoItems Items{ get; }
    }

    [DomainLogic(typeof(IModelTodo))]
    public static class ModelTodoLogic{
        
        public static IObservable<IModelTodo> TodoModel(this IObservable<IModelMicrosoft> source){
            return source.Select(modules => modules.Todo());
        }

        public static IModelTodo Todo(this IModelMicrosoft modelMicrosoft){
            return ((IModelMicrosoftTodo) modelMicrosoft).Todo;
        }

        public static IModelTodo Todo(this IModelOfficeMicrosoft reactiveModules){
            return reactiveModules.Microsoft.Todo();
        }

    }

    public interface IModelTodoItems : IModelList<IModelTodoItem>,IModelNode{
    }

    public interface IModelTodoItem:IModelSynchronizationType,IModelObjectViewDependency{
    }
}
