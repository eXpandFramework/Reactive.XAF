using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.XtraRichEdit;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests.DocumentStyleManager{
    public class ShowServiceTests:BaseTests{
        
        [Test][XpandTest()]
        public void Is_Inactive_ByDefault(){
            using var application=DocumentStyleManagerModule().Application;
            var window = application.CreateViewWindow();
            window.SetDetailView(typeof(DataObject));


            window.Action<DocumentStyleManagerModule>().ShowStyleManager().Active.ResultValue.ShouldBeFalse();
        }
        
        [Test][XpandTest()]
        public void Is_Active_When_EnableShowStyleManger_model_attribute(){
            using var application=DocumentStyleManagerModule().Application;
            application.Model.EnableShowStyleManager();
            var window = application.CreateViewWindow();
            window.SetDetailView(typeof(DataObject));
            
            window.Action<DocumentStyleManagerModule>().ShowStyleManager().Active.ResultValue.ShouldBeTrue();
        }

        [Test][Apartment(ApartmentState.STA)][XpandTest()]
        public void Displays_The_DocumentStyleManager_DetailView_Two_Times(){
            using var application=DocumentStyleManagerModule().Application;
            application.Model.EnableShowStyleManager();
            for (int i = 0; i < 2; i++){
                using var window = application.CreateViewWindow();
                var dataObject = application.CreateObjectSpace().CreateObject<DataObject>();
                dataObject.Content=new byte[]{1,2,3};
                using (var detailView = window.Application.CreateDetailView(dataObject)){
                    window.SetView(detailView);
            
                    var whenDetailViewCreated = application.WhenDetailViewCreated().FirstAsync().SubscribeReplay();
            
                    var singleChoiceAction = window.Action<DocumentStyleManagerModule>().ShowStyleManager();
                    singleChoiceAction.WhenExecuted().Do(e => e.ShowViewParameters.TargetWindow = TargetWindow.NewWindow).FirstAsync().Test();
                    singleChoiceAction.DoExecute(singleChoiceAction.Items.First());

                    var styleManager = ((BusinessObjects.DocumentStyleManager) whenDetailViewCreated.Test().Items.First().e.View.CurrentObject);
                    styleManager.ShouldNotBeNull();
                    styleManager.Content.ShouldBe(dataObject.Content);
                    detailView.Close();
                }

                window.Close();
            }
            
            
        }

        [Test][Apartment(ApartmentState.STA)][XpandTest()]
        public void Displays_List_of_all_Documents_Styles(){
            using var application=DocumentStyleManagerModule().Application;
            application.Model.EnableShowStyleManager();
            var window = application.CreateViewWindow();
            var dataObject = application.CreateObjectSpace().CreateObject<DataObject>();
            Document.NewDocumentStyle(1, DocumentStyleType.Paragraph);
            dataObject.Content=Document.ToByteArray(DocumentFormat.OpenXml);
            var styleManagerViewObserver = application.WhenViewOnFrame(typeof(BusinessObjects.DocumentStyleManager)).Test();
            window.SetView(window.Application.CreateDetailView(dataObject));
            var singleChoiceAction = window.Action<DocumentStyleManagerModule>().ShowStyleManager();
            singleChoiceAction.WhenExecuted().Do(e => e.ShowViewParameters.TargetWindow = TargetWindow.NewWindow).FirstAsync().Test();
            singleChoiceAction.DoExecute(singleChoiceAction.Items.First());

            var documentStyleManager = ((BusinessObjects.DocumentStyleManager) ((DetailView) styleManagerViewObserver.Items.First().View).CurrentObject);
            
            documentStyleManager.AllStyles.Count.ShouldBeGreaterThan(0);
            documentStyleManager.ReplacementStyles.Intersect(documentStyleManager.AllStyles).Count().ShouldBe(documentStyleManager.AllStyles.Count);
        }

    }

}