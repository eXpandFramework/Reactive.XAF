using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Utils;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.Extensions.XAF.ActionExtensions{
    
    public static partial class ActionExtensions{
        public static BoolList Clone(this BoolList boolList){
            var list = new BoolList();
            foreach (var key in boolList.GetKeys()){
                list.SetItemValue(key,boolList[key]);
            }

            return list;
        }

        public static void ExecuteIfAvailable(this SingleChoiceAction actionBase, ChoiceActionItem selectedItem) {
            if (actionBase.Available()) {
                actionBase.DoExecute(selectedItem);
            }
        }
        public static void ExecuteIfAvailable(this SimpleAction actionBase) {
            if (actionBase.Available()) {
                actionBase.DoExecute();
            }
        }
        public static void ExecuteIfAvailable(this ParametrizedAction actionBase) {
            if (actionBase.Available()) {
                actionBase.DoExecute(actionBase.Value);
            }
        }

        public static void DoExecute(this SingleChoiceAction action, object data) 
            => action.DoExecute(action.Items.FirstOrDefault(item => item.Data==data));

        public static void DoExecute(this SingleChoiceAction action,ChoiceActionItem selectedItem, params object[] objectSelection) {
            var context = action.SelectionContext;
            if (objectSelection.Length > 1) {
                throw new NotImplementedException();
            }

            action.SelectionContext = new SelectionContext(objectSelection.Single());
            action.DoExecute(selectedItem);
            action.SelectionContext=context;
        }

        [SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        sealed class SelectionContext:ISelectionContext {
            public SelectionContext(object currentObject) {
                CurrentObject = currentObject;
                SelectedObjects = new List<object>(){currentObject};
                OnCurrentObjectChanged();
                OnSelectionChanged();
            }
            public object CurrentObject { get; set; }
            public IList SelectedObjects { get; set; }
            public SelectionType SelectionType => SelectionType.MultipleSelection;
            public string Name => null;
            public bool IsRoot => false;
            public event EventHandler CurrentObjectChanged;
            public event EventHandler SelectionChanged;
            public event EventHandler SelectionTypeChanged;
            [SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
            void OnSelectionTypeChanged() => SelectionTypeChanged?.Invoke(this, EventArgs.Empty);
            private void OnSelectionChanged() => SelectionChanged?.Invoke(this, EventArgs.Empty);
            private void OnCurrentObjectChanged() => CurrentObjectChanged?.Invoke(this, EventArgs.Empty);
        }
        public static bool DoTheExecute(this ActionBase actionBase,bool force=false) {
            BoolList active = null;
            BoolList enable = null;
            if (force&& (!actionBase.Active||!actionBase.Enabled)){
                active = actionBase.Active.Clone();
                enable = actionBase.Enabled.Clone();
                if (!actionBase.Active){
                    actionBase.Active.Clear();
                }
                if (!actionBase.Enabled){
                    actionBase.Enabled.Clear();
                }
            }
            if (!actionBase.Active||!actionBase.Enabled)
                return false;
            var simpleAction = actionBase as SimpleAction;
            simpleAction?.DoExecute();
            var singleChoiceAction = actionBase as SingleChoiceAction;
            singleChoiceAction?.DoExecute(singleChoiceAction.SelectedItem??singleChoiceAction.Items.FirstOrDefault());

            if (actionBase is PopupWindowShowAction popupWindowShowAction) {
                if (popupWindowShowAction.Application.GetPlatform() == Platform.Win) {
                    var helper = (IDisposable)Activator.CreateInstance(AppDomain.CurrentDomain.GetAssemblyType("DevExpress.ExpressApp.Win.PopupWindowShowActionHelper"),popupWindowShowAction);
                    var view = actionBase.View();
                    void OnClosing(object sender, EventArgs args) {
                        helper.Dispose();
                        view.Closing -= OnClosing;
                    }
                    view.Closing += OnClosing;
                    helper.CallMethod("ShowPopupWindow");
                }
                else {
                    popupWindowShowAction.DoExecute((Window)popupWindowShowAction.Controller.Frame);
                }
            }

            var parametrizedAction = actionBase as ParametrizedAction;
            parametrizedAction?.DoExecute(parametrizedAction.Value);
            if (active != null){
                foreach (var key in active.GetKeys()){
                    active.SetItemValue(key,active[key]);
                }
            }
            if (enable != null){
                foreach (var key in enable.GetKeys()){
                    enable.SetItemValue(key,enable[key]);
                }
            }
            return true;
        }

    }
}