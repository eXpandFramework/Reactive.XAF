using System;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.ObjectTemplate.Tests.BOModel;
using Xpand.XAF.Modules.ObjectTemplate.Tests.Common;

namespace Xpand.XAF.Modules.ObjectTemplate.Tests {
    public class ObjectTemplateTests:CommonAppTest {

        [Test][Order(100)]
        public async Task Template_From_Object() {
            var objectSpace = Application.CreateObjectSpace();
            for (int i = 0; i < 3; i++) {
                var ot = objectSpace.CreateObject<OT>();
                ot.Order = (i + 1) * 10;
            }
            objectSpace.CommitChanges();
            var objectTemplate = objectSpace.CreateObject<BusinessObjects.ObjectTemplate>();
            objectTemplate.ModelType = new ObjectType(typeof(OT));
            objectTemplate.Template = $"@Model.{nameof(OT.Order)}";

            var render = await objectTemplate.Render();
            
            render.ShouldBe("302010");
        }

        [Test][Order(200)]
        public void Render_Template_When_ObjectTemplate_DetailView_Shown() {
            var objectSpace = Application.CreateObjectSpace();
            objectSpace.CreateObject<OT>();
            var objectTemplate = objectSpace.CreateObject<BusinessObjects.ObjectTemplate>();
            objectTemplate.ModelType = new ObjectType(typeof(OT));
            objectTemplate.Template = $"@Model.{nameof(OT.Order)}";
            objectSpace.CommitChanges();
            var testObserver = ObjectTemplateService.CustomTemplateRender.Test();
            var window = Application.CreateViewWindow();
            
            window.SetView(Application.CreateDetailView(objectSpace,objectTemplate));

            testObserver.ItemCount.ShouldBe(1);
        }
        [Test][Order(300)]
        public void Render_Template_When_ObjectTemplate_in_DetailView_Change() {
            var objectSpace = Application.CreateObjectSpace();
            objectSpace.CreateObject<OT>();
            var objectTemplate = objectSpace.CreateObject<BusinessObjects.ObjectTemplate>();
            objectTemplate.ModelType = new ObjectType(typeof(OT));
            objectSpace.CommitChanges();
            var window = Application.CreateViewWindow();
            window.SetView(Application.CreateDetailView(objectSpace,objectTemplate));
            var testObserver = ObjectTemplateService.CustomTemplateRender.Test();
            
            objectTemplate.Template = $"@Model.{nameof(OT.Order)}";
            
            testObserver.ItemCount.ShouldBe(1);
        }

        [Test][Order(400)]
        public void WhenError() {
            throw new NotImplementedException();
        }


    }
}