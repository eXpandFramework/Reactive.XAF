using System.Diagnostics.CodeAnalysis;
using DevExpress.Persistent.Base.General;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common.Attributes;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo.Tests{
    [NonParallelizable]
    public class ModelLogicTests : BaseTest{
        [Test][XpandTest()]
        [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
        public void ObjectView_Lookup_lists_user_string_members(){
            new MicrosoftTodoModule();
            ModelObjectViewDependencyLogic.ObjectViewsMap[typeof(IModelTodo)].ShouldBe(typeof(ITask));
        }

    }
}