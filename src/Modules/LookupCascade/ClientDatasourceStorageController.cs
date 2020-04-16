// using System;
// using System.Linq;
// using System.Reactive.Linq;
// using System.Web;
// using System.Web.UI;
// using System.Web.UI.WebControls;
// using DevExpress.ExpressApp;
// using DevExpress.ExpressApp.Model;
// using DevExpress.ExpressApp.Web;
// using DevExpress.ExpressApp.Web.Templates;
// using Newtonsoft.Json;
// using Xpand.Extensions.String;
// using Xpand.Extensions.XAF.Model;
// using Xpand.XAF.Modules.ASPxLookupCascadePropertyEditor;
//
// [assembly: WebResource(ClientDatasourceStorageController.PakoScriptResourceName, "application/x-javascript")]
// [assembly: WebResource(ClientDatasourceStorageController.ASPxClientLookupPropertyEditorScriptResourceName, "application/x-javascript")]
// namespace Xpand.XAF.Modules.ASPxLookupCascadePropertyEditor{
//     public class ClientDatasourceStorageController:WindowController,IXafCallbackHandler{
//         internal const string PakoScriptResourceName = "Xpand.XAF.Modules.ASPxLookupCascadePropertyEditor.pako.min.js";
//         internal const string ASPxClientLookupPropertyEditorScriptResourceName = "Xpand.XAF.Modules.ASPxLookupCascadePropertyEditor.ASPxLookupCascadePropertyEditor.js";
//         public ClientDatasourceStorageController(){
//             TargetWindowType=WindowType.Main;
//         }
//
//         protected override void OnDeactivated(){
//             base.OnDeactivated();
//             ((WebWindow)Window).ControlsCreating -=OnControlsCreating;
//         }
//
//         protected override void OnActivated(){
//             base.OnActivated();
//             // var webWindow = ((WebWindow)Window);
//             // webWindow.ControlsCreating +=OnControlsCreating;
//             // var modelClientDatasource = ((IModelOptionsClientDatasource) Application.Model.Options).ClientDatasource;
//             // var clientStorage = modelClientDatasource.ClientStorage.ToString().FirstCharacterToLower();
//             // webWindow.RegisterStartupScript($"RequestDatasources_{GetType().FullName}",
//             //     $"RequestDatasources('{GetType().FullName}','{clientStorage}');globalCallbackControl.BeginCallback.AddHandler(function(){{ClearEditorItems('{clientStorage}');}});");
//             // webWindow.RegisterClientScriptResource(GetType(),PakoScriptResourceName);
//             // webWindow.RegisterClientScriptResource(GetType(),ASPxClientLookupPropertyEditorScriptResourceName);
//         }
//         
//         private XafCallbackManager CallbackManager => ((ICallbackManagerHolder)WebWindow.CurrentRequestPage).CallbackManager;
//
//         private void OnControlsCreating(object sender, EventArgs e){
//             // CallbackManager.CallbackControl.Callback+=(source, args) => args.
//             CallbackManager.RegisterHandler(GetType().FullName, this);
//         }
//
//         public void ProcessAction(string parameter){
//             foreach (var d in Application.CreateClientDataSource()){
//                 ((WebWindow) Window).RegisterStartupScript($"StoreDatasource_{d.viewId}",
//                     $"StoreDatasource('{d.viewId}','{d.objects}','{d.storage}')");
//             }
//         }
//     }
// }