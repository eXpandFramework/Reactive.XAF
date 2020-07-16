using System.Diagnostics.CodeAnalysis;
using DevExpress.Persistent.Base.General;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar.Tests{
    [NonParallelizable]
    public class ModelLogicTests : BaseTest{
        [Test][XpandTest()]
        [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
        public void ObjectView_Lookup_lists_user_string_members(){
            new MicrosoftCalendarModule();
            ModelObjectViewDependencyLogic.ObjectViewsMap[typeof(IModelCalendar)].ShouldBe(typeof(IEvent));
        }

    }
}