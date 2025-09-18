using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using Xpand.Extensions.DateTimeExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Speech.BusinessObjects;
using View = DevExpress.ExpressApp.View;

namespace Xpand.XAF.Modules.Speech.Services {
    static class SpeechTextInfoService {
        internal static IObservable<Unit> ConnectSpeechTextInfo(this ApplicationModulesManager manager)
            => manager.NewSpeechTextInfo().Merge(manager.CopySpeechTextInfoPath());

        private static IObservable<Unit> NewSpeechTextInfo(this ApplicationModulesManager manager) 
            => manager.WhenSpeechApplication(application => application.WhenFrame(typeof(SpeechToText),ViewType.DetailView)
                    .SelectUntilViewClosed(frame => frame.View.ToDetailView().NestedFrameContainers(typeof(SpeechText))
                        .SelectMany(container => container.Frame.View.WhenSelectionChanged().Throttle(TimeSpan.FromSeconds(1)).ObserveOnContext()
                            .Select(view => view.SelectedObjects.Cast<SpeechText>().ToArray())
                            .StartWith(container.Frame.View.SelectedObjects.Cast<SpeechText>().ToArray()).WhenNotEmpty()
                            .Do(speechTexts => frame.View.NewSpeechInfo(container.Frame.View.ObjectTypeInfo.Type,speechTexts)))))
                .ToUnit();
        private static IObservable<Unit> CopySpeechTextInfoPath(this ApplicationModulesManager manager) 
            => manager.WhenSpeechApplication(application => application.WhenFrame(typeof(SpeechToText),ViewType.DetailView))
                .SelectMany(frame => frame.View.ToDetailView().WhenNestedListViewProcessCustomizeShowViewParameters(typeof(SSMLFile))
                    .Select(e => {
                        var ssmlFile = (SSMLFile)e.ShowViewParameters.CreatedView.CurrentObject;
                        Clipboard.SetText(ssmlFile.File.FullName);
                        e.ShowViewParameters.CreatedView = null;
                        return ssmlFile;
                    })
                    .ShowXafInfoMessage(file => $"File {file.File.FileName} copied in memory."))
                .ToUnit();

        private static void NewSpeechInfo(this View view,Type speechTextType, params SpeechText[] speechTexts) {
            var speechToText = (SpeechToText)view.CurrentObject;
            var speechTextInfos = speechToText.SpeechInfo;
            string type=speechTextType==typeof(SpeechTranslation)?"Translation":"Recognition";
            speechTextInfos.Remove(speechTextInfos.FirstOrDefault(info => info.SpeechType == type));
            var speechTextInfo = speechToText.ObjectSpace.AdditionalObjectSpace(typeof(SpeechTextInfo)).CreateObject<SpeechTextInfo>();
            speechTextInfo.SpeechType = type;
            speechTextInfo.SelectedLines = speechTexts.Length;
            speechTextInfo.TotalLines = view.ToDetailView().FrameContainers( speechTextType).First()
                .View?.ToListView().CollectionSource.Objects().Count()??0;
            speechTextInfo.Duration = speechTexts.Sum(text => text.Duration.Ticks).TimeSpan();
            speechTextInfo.VoiceDuration = speechTexts.Sum(text => text.VoiceDuration?.Ticks??0).TimeSpan();
            var lastSpeechText = speechTexts.LastOrDefault();
            speechTextInfo.SSMLDuration = lastSpeechText?.Duration.Add(lastSpeechText.Start)??TimeSpan.Zero;
            speechTextInfo.SSMLVoiceDuration = lastSpeechText?.VoiceDuration?.Add(lastSpeechText.Start)??TimeSpan.Zero;
            speechTextInfo.OverTime = speechTexts.Sum(text => (text.VoiceDuration?.Subtract(text.Duration).Ticks??0)).TimeSpan();
            speechTextInfos.Add(speechTextInfo);
            speechTextInfo.RemoveFromModifiedObjects();
        }

    }
}