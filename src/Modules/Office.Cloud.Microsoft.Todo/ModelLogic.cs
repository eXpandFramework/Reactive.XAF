using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Base.General;
using JetBrains.Annotations;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Office.Cloud.Microsoft;
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
        
        [Category("User")][DataSourceProperty(nameof(TodoListNameMembers))]
        IModelMember TodoListNameMember{ get; set; }
        [Browsable(false)]
        IModelList<IModelMember> TodoListNameMembers{ get; }

    }

    [DomainLogic(typeof(IModelTodo))]
    public static class ModelTodoLogic{
        
        public static IObservable<IModelTodo> TodoModel(this IObservable<IModelMicrosoft> source){
            return source.Select(modules => modules.Todo());
        }


        public static IModelObjectViewsDependencyList ObjectViews(this IModelTodo modelTodo){
            return ((IModelObjectViews) modelTodo).ObjectViews;
        }

        public static IModelTodo Todo(this IModelMicrosoft modelMicrosoft){
            return ((IModelMicrosoftTodo) modelMicrosoft).Todo;
        }

        public static IModelTodo Todo(this IModelOfficeMicrosoft reactiveModules){
            return reactiveModules.Microsoft.Todo();
        }

        public static IModelList<IModelMember> Get_TodoListNameMembers(this IModelTodo modelTodo){
            var modelClass = modelTodo.GetParent<IModelOffice>().User;
            var modelMembers =modelClass!=null? modelClass.AllMembers.Where(member => member.Type==typeof(string)):Enumerable.Empty<IModelMember>();
            
            return new CalculatedModelNodeList<IModelMember>(modelMembers);
        }
    }

}
