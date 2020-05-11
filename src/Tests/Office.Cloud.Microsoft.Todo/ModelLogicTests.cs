using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DevExpress.Persistent.Base.General;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Office.Cloud.Microsoft;
using Xpand.Extensions.XAF.Model;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo.Tests{
    [NonParallelizable]
    public class ModelLogicTests : BaseTest{
        // [Test][XpandTest()]
        public void User_lookup_lists_usertypes(){
            using (var application = Platform.Web.TodoModule(nameof(User_lookup_lists_usertypes)).Application){
                var officeModel = application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office;

                var modelClasses = officeModel.Get_Users();
                modelClasses.Count.ShouldBe(1);
                modelClasses.First().TypeInfo.Type.ShouldBe(typeof(PermissionPolicyUser));
            }

        }
        // [Test][XpandTest()]
        public void TodoListNameMember_Lookup_lists_user_string_members(){
            using (var application = Platform.Web.TodoModule(nameof(TodoListNameMember_Lookup_lists_user_string_members)).Application){
                var modelOffice = application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office;
                modelOffice.User = modelOffice.Application.BOModel.GetClass(typeof(PermissionPolicyUser));
                var modelMicrosoft = modelOffice.Microsoft();

                var userMembers = modelOffice.User.AllMembers
                    .Where(info => info.MemberInfo.MemberType==typeof(string)).ToArray();
                var modelMembers = modelMicrosoft.Todo().Get_TodoListNameMembers();
                modelMembers.Count.ShouldBe(userMembers.Count());
                foreach (var userMember in userMembers){
                    modelMembers.Select(member => member.MemberInfo).ShouldContain(userMember.MemberInfo);    
                }
            }
        }
        // [Test][XpandTest()]
        // [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
        public void ObjectView_Lookup_lists_user_string_members(){
            new MicrosoftTodoModule();
            ModelObjectViewDependencyLogic.ObjectViewsMap[typeof(IModelTodo)].ShouldBe(typeof(ITask));
        }

    }
}