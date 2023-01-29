using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Forms;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Speech.BusinessObjects;

namespace Xpand.XAF.Modules.Speech.Services {
    public static class SpeechKeywordService {

        public static IObservable<Unit> ConnectSpeechKeyword(this ApplicationModulesManager manager) {
            return manager.NavigateNextWord().Merge(manager.AddSpeechKeyword())
                .Merge(manager.WhenSpeechApplication(application => application
                    .WhenFrame(SpeechText.SpeechTextKeywordListView)
                    .SelectMany(frame => frame.View.WhenCurrentObjectChanged().Take(1)
                        .Select(view => view.ToListView().HideWhenMasterDetail())
                        .Do(_ => {
                            var collectionSourceBase = frame.View.ToListView().CollectionSource;
                            var lastSpeech = collectionSourceBase.Objects<SpeechText>().OrderByDescending(text => text.Start).First().Oid;
                            collectionSourceBase.SetCriteria<SpeechText>("selection",text => text.Oid==lastSpeech);
                        })
                        
                    )
                ).ToUnit());
        }

        static IObservable<Unit> NavigateNextWord(this ApplicationModulesManager manager) 
            => manager.RegisterViewSimpleAction(nameof(NavigateNextWord), action => {
                    action.TargetObjectType = typeof(SpeechText);
                    action.TargetViewType = ViewType.ListView;
                    action.SelectionDependencyType = SelectionDependencyType.Independent;
                    action.Shortcut = "F12";
                    action.TargetViewId = SpeechText.SpeechTextKeywordListView;
                })
                .WhenConcatExecution(_ => AppDomain.CurrentDomain.UseClipboard(() => {
                    SendKeys.SendWait("^{c}");
                    var selection = Clipboard.GetText();
                    if (!selection.IsNullOrEmpty()) {
                        SendKeys.SendWait("{Right}");
                        SendKeys.SendWait("^+{Right}");
                    }
                }))
                .ToUnit();

        static IObservable<Unit> AddSpeechKeyword(this ApplicationModulesManager manager) {
            return manager.RegisterViewSimpleAction(nameof(AddSpeechKeyword), action => {
                    action.TargetObjectType = typeof(SpeechText);
                    action.TargetViewType = ViewType.ListView;
                    action.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
                    action.Shortcut = "F11";
                    action.TargetViewId = SpeechText.SpeechTextKeywordListView;
                })
                .WhenConcatExecution(e => {
                    SendKeys.SendWait("^{c}");
                    var text = $"{Clipboard.GetText()}".Trim().Trim(',').Trim('.').Trim();
                    var objectSpace = e.View().ObjectSpace;
                    var speechKeyword = objectSpace.DefaultSpeechKeyword(e.Application().Model.SpeechModel());
                    if ($"{speechKeyword.Text}".Split(';').All(key => key != text)) {
                        speechKeyword.Text += $";{text}";
                        speechKeyword.Text = speechKeyword.Text.TrimStart(';');
                        return speechKeyword.Commit().To(speechKeyword).ObserveOnContext()
                            .ShowXafMessage(keyword => $"{text} added to {keyword} {keyword.Text.Split(';').Length}");
                    }
                    return Observable.Empty<SpeechKeyword>();

                })
                .ToUnit();

        }

        internal static SpeechKeyword DefaultSpeechKeyword(this IObjectSpace objectSpace,IModelSpeech modelSpeech) 
            => objectSpace.EnsureObject<SpeechKeyword>(CriteriaOperator.Parse(modelSpeech.DefaultSpeechKeywordCriteria));
    }
}