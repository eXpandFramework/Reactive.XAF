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
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Speech.BusinessObjects;

namespace Xpand.XAF.Modules.Speech.Services {
    public static class TextToSpeechService {
        public static SingleChoiceAction SpeakText(this (SpeechModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(SpeakText)).To<SingleChoiceAction>();
		
        internal static IObservable<Unit> ConnectTextToSpeech(this  ApplicationModulesManager manager)
            => manager.Speak()
        ;

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static IObservable<Unit> Speak(this ApplicationModulesManager manager) {
            return manager.RegisterViewSingleChoiceAction(nameof(SpeakText), action => {
                    action.ImageName = "Action_Debug_Start";
                    action.Items.Add(new ChoiceActionItem("Speak", "Speak"));
                    action.Items.Add(new ChoiceActionItem("Type", "Type"));
                },PredefinedCategory.Tools)
                .WhenConcatExecution(e => e.Speak().Merge(e.Type()))
                .ToUnit();
        }

        private static IObservable<Unit> Speak(this SingleChoiceActionExecuteEventArgs e) 
            => Observable.If(() => (string)e.SelectedChoiceActionItem.Data=="Speak",e.Defer(() => e.Application()
                .UseProviderObjectSpace(space => space.DefaultAccount(e.Application())
                    .Speak( e.Application().Model.SpeechModel(),(result, path) => space.CreateObject<TextToSpeech>()
                        .UpdateSSMLFile(result, path).Commit()).ObserveOnContext()
                    .Do(speech => Clipboard.SetText(speech.File.FullName))
                    .ShowXafMessage(e.Application(),speech => $"{speech.File.FileName} saved and path in memory.")
                )))
                .ToUnit();
        private static IObservable<Unit> Type(this SingleChoiceActionExecuteEventArgs e) 
            => Observable.If(() => (string)e.SelectedChoiceActionItem.Data=="Type",e.Defer(() => {
                e.ShowViewParameters.Controllers.Add(e.Application().CreateController<DialogController>());
                e.ShowViewParameters.CreateAllControllers = true;
                e.ShowViewParameters.CreatedView = e.Application().NewDetailView(space => space.CreateObject<TextToSpeech>(),
                    (IModelDetailView)e.Application().Model.Views[TextToSpeech.TypeSpeakDetailView]);
                e.ShowViewParameters.NewWindowTarget=NewWindowTarget.Separate;
                e.ShowViewParameters.TargetWindow=TargetWindow.NewModalWindow;
            }))
            .ToUnit();
    }
}