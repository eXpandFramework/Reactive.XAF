using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Speech.Services {
    public static class TextToSpeechService {
        public static SimpleAction SpeakText(this (SpeechModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(SpeakText)).To<SimpleAction>();
		
        internal static IObservable<Unit> ConnectTextToSpeech(this  ApplicationModulesManager manager)
            => manager.Speak()
        ;

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static IObservable<Unit> Speak(this ApplicationModulesManager manager) {
            return manager.RegisterViewSimpleAction(nameof(SpeakText), action => {
                    action.Shortcut = "ctrlalts";
                    action.ImageName = "Action_Debug_Start";
                },PredefinedCategory.Tools)
                .WhenConcatExecution(e => e.Application().UseProviderObjectSpace(space => space.DefaultAccount(e.Application())
                    .Speak( e.Application().Model.SpeechModel())))
                .ToUnit();
        }
        
        

    }
}