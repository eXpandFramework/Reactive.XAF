using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Web;
using DevExpress.ExpressApp.Web.Editors.ASPx;
using DevExpress.ExpressApp.Web.Utils;
using Xpand.Extensions.String;
using Xpand.Extensions.XAF.Model;

namespace Xpand.XAF.Modules.ClientLookupCascade{
    public class ParentViewLookupItemsController : ViewController<ObjectView>{
        private IModelMemberViewItem[] _viewItems;

        protected override void OnActivated(){
            base.OnActivated();
            _viewItems = View.Model.MemberViewItems(typeof(ASPxClientLookupCascadePropertyEditor));
            if (_viewItems.Any()){
                var editors=Enumerable.Empty<ASPxClientLookupCascadePropertyEditor>().ToArray();
                if (View is DetailView detailView&& detailView.ViewEditMode==ViewEditMode.Edit){
                    editors = detailView.GetItems<ASPxClientLookupCascadePropertyEditor>().ToArray();
                }
                ConfigureEditors(editors);
            }
        }

        private void ConfigureEditors(ASPxClientLookupCascadePropertyEditor[] editors){
            foreach (var editor in editors){
                ((WebWindow) Application.MainWindow).RegisterClientScript($"{View.Id}_LookupEditors",
                    $"Window.{View.Id}_LookupEditors=new Array();");
                editor.ParentView = View;
            }
        }

        protected override void OnViewControlsCreated(){
            base.OnViewControlsCreated();
            if (_viewItems.Any()){
                if (View is ListView listView && listView.Editor is ASPxGridListEditor gridListEditor){
                    var editors = _viewItems.Select(item => (ASPxClientLookupCascadePropertyEditor) gridListEditor.FindPropertyEditor(item, ViewEditMode.Edit))
                        .Where(editor => editor!=null).ToArray();
                    var clientStorage = ((IModelOptionsClientDatasource) View.Model.Application.Options)
                        .ClientDatasource.ClientStorage.ToString().FirstCharacterToLower();
                    ClientSideEventsHelper.AssignClientHandlerSafe(gridListEditor.Grid, nameof(gridListEditor.Grid.ClientSideEvents.BeginCallback),
                        $"function(s,e){{ClearEditorItems('{clientStorage}','{View}')}}", $"{GetType().FullName}{nameof(gridListEditor.Grid.ClientSideEvents.BeginCallback)}");
                    ConfigureEditors(editors);
                }
                
                
            }
        }
    }
}