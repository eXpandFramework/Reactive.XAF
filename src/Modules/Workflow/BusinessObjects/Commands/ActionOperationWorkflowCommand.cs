
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Workflow.Services;

namespace Xpand.XAF.Modules.Workflow.BusinessObjects.Commands{
    [DefaultProperty(nameof(Description))]
    [System.ComponentModel.DisplayName("Action Operation")]
    [ImageName(nameof(ActionOperationWorkflowCommand))]
    public class ActionOperationWorkflowCommand(Session session) :WorkflowCommand(session){
        protected internal override int GetIndex(){
            var startActions = new[] { StartAction }
                .WhereNotDefault()
                .Concat(StartCommands.WhereNotDefault()).ToArray();
            return startActions.Any() ? 1 + startActions.Max(c => c.GetIndex()) : 0;
            
        }


        public override IObservable<object[]> Execute(XafApplication application, params object[] objects) 
            => Observable.Defer(() => application.WhenActionExecuted(Action).ObserveOnDefault()
                    .SelectMany(e => {
                        var view = e.View();
                        return view.Id == View?.Name || View == null ? Emission switch {
                                ActionOperationWorkflowCommandEmission.SelectedObjects => view.SelectedObjects.Cast<object>()
                                    .Select(o => !OutputProperty.IsNotNullOrEmpty() ? o : 
                                        view.ObjectTypeInfo.FindMember(OutputProperty)?.GetValue(o)).WhereNotDefault().Distinct().ToArray().Observe(),
                                ActionOperationWorkflowCommandEmission.ViewObjects => view.Objects()
                                    .Select(o => !OutputProperty.IsNotNullOrEmpty() ? o
                                        : view.ObjectTypeInfo.FindMember(OutputProperty)?.GetValue(o)).WhereNotDefault().Distinct().ToArray().Observe(),
                                _ => new object[] { e.Action }.Observe()
                            }
                            : Observable.Empty<object[]>();
                    }))
                
                // .Repeat()
                // .RepeatWhen(bus => bus.SelectMany(o => CommandSuite.Commands.Where(command => command.Oid!=this.Oid).ToObservable().SelectMany(command => command.WhenExecuted()))) 
            ;

        protected override Type GetReturnType() => typeof(ActionBase);

        protected override bool GetNeedSubscription() => false;

        string _outputProperty;
        [ToolTip("Optional. Specifies a property to project from the output objects. If set, the output will be the property values instead of the full objects. Supports formatted strings like 'ID: {Oid}'.")]
        public string OutputProperty{
            get => _outputProperty;
            set => SetPropertyValue(nameof(OutputProperty), ref _outputProperty, value);
        }

        ActionOperationWorkflowCommandEmission _emission;
        [ToolTip("Controls what the command outputs when the action is executed: the Action itself, the View's selected objects, or all objects in the View.")]
        public ActionOperationWorkflowCommandEmission Emission {
            get => _emission;
            set => SetPropertyValue(nameof(Emission), ref _emission, value);
        }
        
        [ToolTip("The ID of the XAF Action that triggers this command.")]
        ObjectString _action;
        [DataSourceProperty(nameof(Actions))]
        [RuleRequiredField]
        [ValueConverter(typeof(ObjectStringValueConverter))]
        [Persistent]
        [ToolTip("The ID of the XAF Action that triggers this command.")]
        public ObjectString Action {
            get => _action;
            set => SetPropertyValue(nameof(Action), ref _action, value);
        }
        
        [Browsable(false)]
        public IList<ObjectString> Actions 
            => CaptionHelper.ApplicationModel.ActionDesign.Actions
                .Select(action => action.GetValue<ActionBase>(ModelActionsNodesGenerator.ActionPropertyName))
                .Select(action => new ObjectString(action.Id) {Caption = action.Caption}).ToArray();

        ObjectString _view;
        [DataSourceProperty(nameof(Views))]
        [ValueConverter(typeof(ObjectStringValueConverter))]
        [Persistent]
        [ToolTip("Optional. The ID of the View where the action must be executed for this command to trigger. If empty, the command triggers in any View.")]
        public ObjectString View {
            get => _view;
            set => SetPropertyValue(nameof(View), ref _view, value);
        }

        [Browsable(false)]
        public IList<ObjectString> Views 
            => CaptionHelper.ApplicationModel.Views
                .Select(modelView => new ObjectString(modelView.Id)).ToArray();
    }

    public enum ActionOperationWorkflowCommandEmission {
        Action,
        SelectedObjects,
        ViewObjects
    }
}