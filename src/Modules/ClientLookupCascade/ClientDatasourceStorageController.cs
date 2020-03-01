using System;
using System.Linq;
using System.Reactive.Linq;
using System.Web;
using System.Web.UI;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Web;
using DevExpress.ExpressApp.Web.Templates;
using Newtonsoft.Json;
using Xpand.Extensions.String;
using Xpand.Extensions.XAF.Model;
using Xpand.XAF.Modules.ClientLookupCascade;

[assembly: WebResource(ClientDatasourceStorageController.PakoScriptResourceName, "application/x-javascript")]
[assembly: WebResource(ClientDatasourceStorageController.ASPxClientLookupPropertyEditorScriptResourceName, "application/x-javascript")]
namespace Xpand.XAF.Modules.ClientLookupCascade{
    public class ClientDatasourceStorageController:WindowController,IXafCallbackHandler{
        internal const string PakoScriptResourceName = "Xpand.XAF.Modules.ClientLookupCascade.pako.min.js";
        internal const string ASPxClientLookupPropertyEditorScriptResourceName = "Xpand.XAF.Modules.ClientLookupCascade.ASPxClientLookupCascadePropertyEditor.js";
        public ClientDatasourceStorageController(){
            TargetWindowType=WindowType.Main;
        }

        protected override void OnDeactivated(){
            base.OnDeactivated();
            ((WebWindow)Window).ControlsCreating -=OnControlsCreating;
        }

        protected override void OnActivated(){
            base.OnActivated();
            var webWindow = ((WebWindow)Window);
            webWindow.ControlsCreating +=OnControlsCreating;
            var modelClientDatasource = ((IModelOptionsClientDatasource) Application.Model.Options).ClientDatasource;
            var clientStorage = modelClientDatasource.ClientStorage.ToString().FirstCharacterToLower();
            webWindow.RegisterStartupScript($"RequestDatasources_{GetType().FullName}",
                $"RequestDatasources('{GetType().FullName}','{clientStorage}');globalCallbackControl.BeginCallback.AddHandler(function(){{ClearEditorItems('{clientStorage}');}});");
            webWindow.RegisterClientScriptResource(GetType(),PakoScriptResourceName);
            webWindow.RegisterClientScriptResource(GetType(),ASPxClientLookupPropertyEditorScriptResourceName);
        }
        
        private XafCallbackManager CallbackManager => ((ICallbackManagerHolder)WebWindow.CurrentRequestPage).CallbackManager;

        private void OnControlsCreating(object sender, EventArgs e){
            CallbackManager.RegisterHandler(GetType().FullName, this);
        }

        public void ProcessAction(string parameter){
            var modelClientDatasource = ((IModelOptionsClientDatasource) Application.Model.Options).ClientDatasource;
            var viewsIds = modelClientDatasource.LookupViews.Select(view => view.LookupListView.Id);
            var data = viewsIds.ToObservable()
                .SelectMany(viewId => {
                    var modelListView = (IModelListView)Application.FindModelView(viewId);
                    var modelColumns = modelListView.VisibleMemberViewItems().OrderForView();
                    return Observable.Start(() => {
                        using (var objectSpace = Application.CreateObjectSpace()){
                            using (var collectionSource = Application.CreateCollectionSource(objectSpace,
                                modelListView.ModelClass.TypeInfo.Type, modelListView.Id, false, CollectionSourceMode.Normal)){
                                var objects = new[] { (object)null }.Concat(collectionSource.List.Cast<object>())
                                    .Select(o => {
                                        var columns = string.Join("&", modelColumns.Select(column =>
                                            HttpUtility.UrlEncode(GetDisplayText(column.ModelMember.MemberInfo.GetValue(o), "N/A", null))));
                                        return new{
                                            Key = o == null ? null : collectionSource.ObjectSpace.GetObjectHandle(o),
                                            Columns = columns
                                        };
                                    })
                                    .ToArray();
                                objects = new[]{
                                    new{
                                        Key = "FieldNames",
                                        Columns = string.Join("&", modelColumns.Select(column => HttpUtility.UrlEncode(column.Caption)))
                                    }
                                }.Concat(objects).ToArray();
                                return new { uniqueId = viewId, objects = Convert.ToBase64String(JsonConvert.SerializeObject(objects).Zip()) };
                            }
                        }

                    });
                }).ToEnumerable();


            foreach (var d in data){
                ((WebWindow) Window).RegisterStartupScript($"StoreDatasource_{d.uniqueId}",
                    $"StoreDatasource('{d.uniqueId}','{d.objects}','{modelClientDatasource.ClientStorage.ToString().FirstCharacterToLower()}')");
            }
        }

        private string GetDisplayText(object editValue, string nullText, string format){
            if (editValue != null){
                var result = editValue;
                if (!string.IsNullOrEmpty(format)) result = string.Format(format, result);
                return result.ToString();
            }

            return nullText;
        }
    }

    
}