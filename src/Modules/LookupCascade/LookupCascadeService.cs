using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using System.Web.UI;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Web;
using Newtonsoft.Json;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.String;
using Xpand.Extensions.XAF.Model;
using Xpand.XAF.Modules.LookupCascade;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

[assembly: WebResource(LookupCascadeService.PakoScriptResourceName, "application/x-javascript")]
[assembly: WebResource(LookupCascadeService.ASPxClientLookupPropertyEditorScriptResourceName, "application/x-javascript")]
namespace Xpand.XAF.Modules.LookupCascade{
    public static class LookupCascadeService{
        public const string FieldNames = "FieldNames";
        public static string NA = "N/A";
        internal const string PakoScriptResourceName = "Xpand.XAF.Modules.LookupCascade.pako.min.js";
        internal const string ASPxClientLookupPropertyEditorScriptResourceName = "Xpand.XAF.Modules.LookupCascade.ASPxLookupCascadePropertyEditor.js";

        internal static IObservable<Unit> Connect(this LookupCascadeModule module) => module.Application.RegisterClientScripts().Merge(module.Application.StoreDataSource());

        private static IObservable<Unit> StoreDataSource(this XafApplication application) =>
            application.WhenWeb().WhenCallBack(typeof(LookupCascadeService).FullName)
                .SelectMany(pattern => application.CreateClientDataSource())
                .Do(_ => {
                    WebWindow.CurrentRequestWindow.RegisterStartupScript($"StoreDatasource_{_.viewId}",
                        $"StoreDatasource('{_.viewId}','{_.objects}','{_.storage}')");
                })
                .TraceLookupCascdeModule()
                .ToUnit();

        private static IObservable<Unit> RegisterClientScripts(this XafApplication application) =>
            application.WhenWindowCreated().Cast<WebWindow>().CombineLatest(application.ReactiveModulesModel().LookupCascadeModel())
                .Do(_ => {
                    var window = _.first;
                    var modelClientDatasource = _.second.ClientDatasource;
                    var clientStorage = modelClientDatasource.ClientStorage.ToString().FirstCharacterToLower();
                    var type = typeof(LookupCascadeService);
                    window.RegisterStartupScript($"RequestDatasources_{type.FullName}",
                        $"RequestDatasources('{type.FullName}','{clientStorage}');globalCallbackControl.BeginCallback.AddHandler(function(){{ClearEditorItems('{clientStorage}');}});");
                    window.RegisterClientScriptResource(type,PakoScriptResourceName);
                    window.RegisterClientScriptResource(type,ASPxClientLookupPropertyEditorScriptResourceName);
                    RegisterPollyFills(window);
                })
                .TraceLookupCascdeModule(_ =>$"{_.first.Context}, {_.second.Id()}" )
                .ToUnit();

        private static void RegisterPollyFills(WebWindow window){
            var pollyFills = new[]{
                "String.prototype.startsWith","String.prototype.endsWith", "Array.prototype.filter", "Object.entries", "Array.prototype.includes",
                "Array.prototype.keys", "Array.prototype.findIndex", "Array.prototype.find"
            };
            window.RegisterClientScriptInclude($"{typeof(LookupCascadeService).FullName}_polyfill",
                $"https://polyfill.io/v3/polyfill.min.js?version=3.52.1&features={string.Join("%2C",pollyFills)}");
        }

        public static IEnumerable<(string viewId, string objects, string storage)> CreateClientDataSource(this XafApplication application){
            var modelClientDatasource = application.ReactiveModulesModel().LookupCascadeModel().Wait().ClientDatasource;
            var storage = modelClientDatasource.ClientStorage.ToString().FirstCharacterToLower();
            var viewsIds = modelClientDatasource.LookupViews.Select(view => view.LookupListView.Id);
            return viewsIds.ToObservable()
                .SelectMany(viewId => application.CreateClientDataSource((IModelListView) application.FindModelView(viewId))).ToEnumerable()
                .Select(_ => (_.viewId,_.objects,storage));
        }

        static IObservable<(string viewId, string objects)> CreateClientDataSource(this XafApplication application,IModelListView modelListView){
            string GetDisplayText(object editValue, string nullText, string format){
                if (editValue != null){
                    var result = editValue;
                    if (!string.IsNullOrEmpty(format)) result = string.Format(format, result);
                    return result.ToString();
                }
                return nullText;
            }

            var modelColumns = modelListView.VisibleMemberViewItems().OrderForView();
            return Observable.Start(() => {
                using (var objectSpace = application.CreateObjectSpace()){
                    using (var collectionSource = application.CreateCollectionSource(objectSpace,
                        modelListView.ModelClass.TypeInfo.Type, modelListView.Id, false, CollectionSourceMode.Normal)){
                        var objects = new[] { (object)null }.Concat(collectionSource.List.Cast<object>())
                            .Select(o => {
                                var columns = string.Join("&", modelColumns.Select(column => {
                                    var value = column.ParentView.AsObjectView.ModelClass.TypeInfo.FindMember(column.PropertyName).GetValue(o);
                                    return HttpUtility.UrlEncode(GetDisplayText(value, NA, null));
                                }));
                                return new{
                                    Key = o == null ? null : collectionSource.ObjectSpace.GetObjectHandle(o),
                                    Columns = columns
                                };
                            })
                            .ToArray();
                        objects = new[]{
                            new{
                                Key = FieldNames,
                                Columns = string.Join("&", modelColumns.Select(column => HttpUtility.UrlEncode(column.Caption)))
                            }
                        }.Concat(objects).ToArray();
                        return ( uniqueId: modelListView.Id, objects: Convert.ToBase64String(JsonConvert.SerializeObject(objects).Zip()) );
                    }
                }

            });
        }
        
        internal static IObservable<TSource> TraceLookupCascdeModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, LookupCascadeModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

    }
    
}