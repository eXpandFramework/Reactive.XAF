using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;
using Xpand.XAF.Modules.Speech.BusinessObjects;

namespace Xpand.XAF.Modules.Speech.Services {
    public static class TextToSpeechService {
        public static SingleChoiceAction SpeakText(this (SpeechModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(SpeakText)).To<SingleChoiceAction>();

        internal static IObservable<Unit> ConnectTextToSpeech(this  ApplicationModulesManager manager)
            => manager.SpeakText()
                .MergeToUnit(manager.WhenSpeechApplication(application =>application.WhenFrameViewChanged()
                    .WhenFrame(typeof(TextToSpeech),ViewType.DetailView)
                    .SelectUntilViewClosed(frame => {
                        if (frame.View.Id != TextToSpeech.TypeSpeakDetailView) {
                            return frame.WhenSaveSpeak().ToUnit();
                        }
                        return frame.View.ObjectSpace.WhenCommittedDetailed<TextToSpeech>(
                                ObjectModification.NewOrUpdated, speech => speech.Text.IsNotNullOrEmpty(),
                                nameof(TextToSpeech.Text))
                            .ToObjects().WaitUntilInactive(3).ObserveOnContext()
                            .SelectMany(speech => frame.Application.Speak(frame.View.ObjectSpace, _ => speech,
                                () => speech.Text).ObserveOnContext().Do(toSpeech => toSpeech.ObjectSpace.Refresh()).ToUnit());

                    })))
                .MergeToUnit(manager.SaveTextToSpeech())
        ;

        private static IObservable<SimpleActionExecuteEventArgs> SaveTextToSpeech(this ApplicationModulesManager manager) 
            => manager.RegisterViewSimpleAction("SaveTextToSpeech",action => {
                    action.TargetViewId = TextToSpeech.TypeSpeakDetailView;
                    action.Shortcut = "Control+S";
                },PredefinedCategory.PopupActions)
                .WhenExecuted().Do(e => e.View().ObjectSpace.CommitChanges());

        private static IObservable<Unit> WhenTypeSpeakOnIdle(this Frame frame)
            => frame.View.ObjectSpace.WhenCommittedDetailed<TextToSpeech>(ObjectModification.NewOrUpdated,text => !text.Text.IsNullOrEmpty(),nameof(TextToSpeech.Text)).ToObjects()
                .WaitUntilInactive(2).ObserveOnContext().Select (textToSpeech => Observable.If(() => !textToSpeech.Text.IsNullOrEmpty(),
                    frame.Application.Speak(frame.View.ObjectSpace, _ => textToSpeech, () => textToSpeech.Text)
                        .Do(speech => speech.ObjectSpace.Refresh())))
                .ToUnit();
        
        
        private static IObservable<SingleChoiceAction> WhenSaveSpeak(this Frame frame) 
            => frame.View.ObjectSpace.WhenCommitted().WaitUntilInactive(3).ObserveOnContext()
                .Select(_ => frame.Application.MainWindow.SingleChoiceAction(nameof(SpeakText)))
                .Do(action => action.Speak());

        private static void Speak(this SingleChoiceAction action) => action.DoExecute("Speak");

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static IObservable<Unit> SpeakText(this ApplicationModulesManager manager) 
            => manager.RegisterWindowSingleChoiceAction(nameof(SpeakText),PredefinedCategory.Tools,action => {
                    action.ImageName = "Action_Debug_Start";
                    action.Items.Add(new ChoiceActionItem("Speak", "Speak"));
                    action.Items.Add(new ChoiceActionItem("Type", "Type"));
                    action.ItemType=SingleChoiceActionItemType.ItemIsOperation;
                })
                .MergeToUnit(manager.WhenSpeechApplication(application => application.WhenFrameCreated(TemplateContext.ApplicationWindow)
                    .SelectMany(frame => frame.SingleChoiceAction(nameof(SpeakText))
                        .WhenExecuted(e => e.Speak().Merge(e.Type())))))
                .ToUnit();

        private static IObservable<Unit> Speak(this SingleChoiceActionExecuteEventArgs e) 
            => Observable.If(() => (string)e.SelectedChoiceActionItem.Data=="Speak",e.Defer(() => e.Application()
                .UseProviderObjectSpace(space => e.Application().Speak(space,objectSpace => objectSpace.CreateObject<TextToSpeech>(),Clipboard.GetText))))
                .ToUnit();

        private static IObservable<TextToSpeech> Speak(this XafApplication application, IObjectSpace space,
            Func<IObjectSpace, TextToSpeech> textToSpeechSelector, Func<string> textSelector) 
            => space.DefaultAccount(application)
                .Speak( application.Model.SpeechModel(),(result, path) => textToSpeechSelector(space)
                    .UpdateSSMLFile(result, path).Commit(),textSelector).ObserveOnContext()
                .Do(speech => Clipboard.SetText(speech.File.FullName))
                .ShowXafMessage(application,speech => $"{speech.File.FileName} saved and path in memory.");

        private static IObservable<Unit> Type(this SingleChoiceActionExecuteEventArgs e) 
            => Observable.If(() => (string)e.SelectedChoiceActionItem.Data=="Type",e.Defer(() => {
                    var dialogController = e.Application().CreateController<DialogController>();
                    e.ShowViewParameters.Controllers.Add(dialogController);
                e.ShowViewParameters.CreateAllControllers = true;
                e.ShowViewParameters.CreatedView = e.Application().NewDetailView(space => space.CreateObject<TextToSpeech>(),
                    (IModelDetailView)e.Application().Model.Views[TextToSpeech.TypeSpeakDetailView]);
                e.ShowViewParameters.NewWindowTarget=NewWindowTarget.Separate;
                e.ShowViewParameters.TargetWindow=TargetWindow.NewModalWindow;
                return dialogController.WhenFrameAssigned().SelectMany(frame => {
                    return frame.GetController<ModificationsController>().WhenActivated().Select(controller => controller.SaveAction.WhenExecuted().Select(args => args));
                });
            }))
            .ToUnit();
    }
}