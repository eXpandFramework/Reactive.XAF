using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Swordfish.NET.Collections.Auxiliary;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.TaskExtensions;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.RazorView.Tests.BOModel;
using Xpand.XAF.Modules.RazorView.Tests.Common;

namespace Xpand.XAF.Modules.RazorView.Tests {
    public class RazorViewTests:CommonAppTest {
        // [XpandTest()]
        [Test][Order(100)]
        public async Task RazorView_From_DataSource() {
            var objectSpace = Application.CreateObjectSpace();
            for (int i = 0; i < 3; i++) {
                var ot = objectSpace.CreateObject<OT>();
                ot.Order = (i + 1) * 10;
            }
            objectSpace.CommitChanges();
            var objectTemplate = objectSpace.CreateObject<BusinessObjects.RazorView>();
            objectTemplate.ModelType = new ObjectType(typeof(OT));
            objectTemplate.Template = $"@Model.{nameof(OT.Order)}";

            // var concurrentHashSet = RazorViewService.MetadataReferences;
            
            var render = await objectTemplate.Render().Timeout(Timeout);
            
            render.ShouldSatisfyAllConditions(() => render.ShouldContain("10"),() => render.ShouldContain("20"),() => render.ShouldContain("30"));
            RemoveObjects(objectSpace);
        }

        [Test][Order(200)][XpandTest()]
        public void Render_When_RazorView_DetailView_Shown() {
            var objectSpace = Application.CreateObjectSpace();
            objectSpace.CreateObject<OT>();
            var objectTemplate = objectSpace.CreateObject<BusinessObjects.RazorView>();
            objectTemplate.ModelType = new ObjectType(typeof(OT));
            objectTemplate.Template = $"@Model.{nameof(OT.Order)}";
            objectSpace.CommitChanges();
            using var testObserver = Application.WhenRazorViewRendering().Test();
            var window = Application.CreateViewWindow();
            
            window.SetView(Application.CreateDetailView(objectSpace,objectTemplate));

            testObserver.ItemCount.ShouldBe(1);
            RemoveObjects(objectSpace);
        }
        [Test][Order(200)][XpandTest()]
        public void Customize_Render_For_RazorView_Object() {
            var objectSpace = Application.CreateObjectSpace();
            objectSpace.CreateObject<OT>();
            var objectTemplate = objectSpace.CreateObject<BusinessObjects.RazorView>();
            objectTemplate.ModelType = new ObjectType(typeof(OT));
            objectTemplate.Template = $"@Model.{nameof(OT.Order)}";
            objectSpace.CommitChanges();
            using var testObserver = Application.WhenRazorViewRendering()
                .Do(e => e.Handled=e.SetInstance(t => {
                    t.renderedView = nameof(Customize_Render_For_RazorView_Object);
                    return t;
                }))
                .Test();
            var window = Application.CreateViewWindow();
            
            window.SetView(Application.CreateDetailView(objectSpace,objectTemplate));

            testObserver.ItemCount.ShouldBe(1);
            objectTemplate.Preview.ShouldBe(nameof(Customize_Render_For_RazorView_Object));
            RemoveObjects(objectSpace);
        }

        private static void RemoveObjects(IObjectSpace objectSpace) {
            objectSpace.Delete(objectSpace.GetObjectsQuery<OT>().ToArray());
            objectSpace.Delete(objectSpace.GetObjectsQuery<BusinessObjects.RazorView>().ToArray());
            objectSpace.CommitChanges();
        }

        [Test][Order(201)][XpandTest()]
        public async Task Customize_Render_For_DataSource_Object() {
            var objectSpace = Application.CreateObjectSpace();
            objectSpace.CreateObject<OT>();
            var objectTemplate = objectSpace.CreateObject<BusinessObjects.RazorView>();
            objectTemplate.ModelType = new ObjectType(typeof(OT));
            objectTemplate.Template = $"@Model.{nameof(OT.Order)}";
            objectSpace.CommitChanges();
            using var testObserver = Application.WhenRazorViewDataSourceObjectRendering()
                .Do(e => e.Handled=e.SetInstance(t => {
                    t.renderedView = nameof(Customize_Render_For_DataSource_Object).Observe()
	                    .Select(s => s).FirstAsync();
                    return t;
                }))
                .Test();
            var window = Application.CreateViewWindow();
            
            window.SetView(Application.CreateDetailView(objectSpace,objectTemplate));

            await Observable.While(() => objectTemplate.Preview == null, Unit.Default.Observe()).DefaultIfEmpty().Timeout(Timeout);
            testObserver.ItemCount.ShouldBe(1);
            objectTemplate.Preview.ShouldBe(nameof(Customize_Render_For_DataSource_Object));
            RemoveObjects(objectSpace);
        }
        
        [Test][Order(300)]
        public void Render_When_RazorView_in_DetailView_Change() {
            var objectSpace = Application.CreateObjectSpace();
            objectSpace.CreateObject<OT>();
            var objectTemplate = objectSpace.CreateObject<BusinessObjects.RazorView>();
            objectTemplate.ModelType = new ObjectType(typeof(OT));
            objectSpace.CommitChanges();
            var window = Application.CreateViewWindow();
            window.SetView(Application.CreateDetailView(objectSpace,objectTemplate));
            using var testObserver = Application.WhenRazorViewRendering().Test();
            
            objectTemplate.Template = $"@Model.{nameof(OT.Order)}";
            
            testObserver.ItemCount.ShouldBe(1);
            RemoveObjects(objectSpace);
        }

        [Test][Order(400)][XpandTest()]
        public void WhenError() {
            var objectSpace = Application.CreateObjectSpace();
            objectSpace.CreateObject<OT>();
            var objectTemplate = objectSpace.CreateObject<BusinessObjects.RazorView>();
            objectTemplate.ModelType = new ObjectType(typeof(OT));
            objectTemplate.Template = $"@Model.{nameof(OT.Order)}1";
            objectSpace.CommitChanges();
            using var testObserver = Application.WhenRazorViewRendering().Test();
            var window = Application.CreateViewWindow();
            
            window.SetView(Application.CreateDetailView(objectSpace,objectTemplate));
            
            objectTemplate.Error.ShouldNotBeNull();
            RemoveObjects(objectSpace);

        }


    }
}