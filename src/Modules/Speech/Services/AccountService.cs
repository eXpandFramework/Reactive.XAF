using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.CognitiveServices.Speech;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.FileExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Speech.BusinessObjects;

namespace Xpand.XAF.Modules.Speech.Services {
    public static class AccountService {
        public static IObservable<Unit> ConnectAccount(this ApplicationModulesManager manager) 
            => manager.UpdateVoices();

        private static IObservable<Unit> UpdateVoices(this ApplicationModulesManager manager) 
            => manager.UpdateVoicesOnViewRefresh().Merge(manager.UpdateVoicesOnCommit());

        private static IObservable<Unit> UpdateVoicesOnViewRefresh(this ApplicationModulesManager manager) 
            => manager.WhenSpeechApplication(application => application.WhenFrame(typeof(BusinessObjects.SpeechService),ViewType.DetailView)
                .SelectUntilViewClosed(frame => frame.GetController<RefreshController>().RefreshAction.WhenConcatRetriedExecution(_ =>frame.View.CurrentObject.To<BusinessObjects.SpeechService>().UpdateVoices() )).ToUnit());

        public static BusinessObjects.SpeechService DefaultSpeechAccount(this IObjectSpace space,IModelSpeech modelSpeech) 
            => space.FindObject<BusinessObjects.SpeechService>(CriteriaOperator.Parse(modelSpeech.DefaultSpeechServiceCriteria));

        public static IObservable<TResult> Speak<TResult>(this BusinessObjects.SpeechService speechService, IModelSpeech speechModel,Func<BusinessObjects.SpeechService,SpeechSynthesisResult,string,IObservable<TResult>> afterBytesWritten,Func<string> textSelector) 
            => Observable.Using(() => new SpeechSynthesizer(speechService.SpeechConfig()), synthesizer
                => synthesizer.SpeakAsync(textSelector).ToObservable().ObserveOnContext()
                    .DoWhen(_ => !new DirectoryInfo(speechModel.DefaultStorageFolder).Exists,
                        _ => Directory.CreateDirectory(speechModel.DefaultStorageFolder))
                    .SelectMany(result => {
                        var path = speechService.ObjectSpace.GetObjectsQuery<TextToSpeech>()
                            .OrderByDescending(speech => speech.Oid).FirstOrDefault()
                            .WavFileName(speechModel,oid => (oid + 1).ToString());
                        path = new FileInfo(path).EnsurePath().FullName;
                        return File.WriteAllBytesAsync(path, result.AudioData).ToObservable().ObserveOnContext()
                            .SelectMany(_ => afterBytesWritten(speechService, result,path)
                                .RetryWithBackoff(3));
                    }));


        private static Task<SpeechSynthesisResult> SpeakAsync(this SpeechSynthesizer synthesizer,Func<string> textSelector) {
            var text = textSelector();
            if (text.StartsWith("<speak")) {
                return synthesizer.SpeakSsmlAsync(text);
            }
            return synthesizer.SpeakTextAsync(text);
        }

        public static SpeechConfig SpeechConfig(this BusinessObjects.SpeechService service,SpeechLanguage recognitionLanguage=null,[CallerMemberName]string callerMember="") {
            var speechConfig = Microsoft.CognitiveServices.Speech.SpeechConfig.FromSubscription(service.Key, service.Region);
            speechConfig.SpeechRecognitionLanguage = $"{recognitionLanguage?.Name}";
            speechConfig.EnableAudioLogging();
            var path = $"{AppDomain.CurrentDomain.ApplicationPath()}\\Logs\\{nameof(SpeechConfig)}{callerMember}.log";
            if (!File.Exists(path)) {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            }
            speechConfig.SetProperty(PropertyId.Speech_LogFilename, path);
            speechConfig.SetProperty(PropertyId.SpeechServiceConnection_EndSilenceTimeoutMs, "5000");
            speechConfig.SetProperty(PropertyId.Conversation_Initial_Silence_Timeout, "5000");
            
            speechConfig.SetProperty(PropertyId.SpeechServiceConnection_InitialSilenceTimeoutMs, "0");
            return speechConfig;
        }

        public static SpeechTranslationConfig TranslationConfig(this BusinessObjects.SpeechService service) 
            => SpeechTranslationConfig.FromSubscription(service.Key, service.Region);
        
        private static void EnsureVoice(this BusinessObjects.SpeechService service,VoiceInfo voiceInfo) {
            var speechVoice = service.ObjectSpace.EnsureObject<SpeechVoice>(voice => voice.Service!=null&& voice.Service.Oid==service.Oid&&voice.Name==voiceInfo.Name,inTransaction:true);
            speechVoice.Gender = voiceInfo.Gender;
            speechVoice.Language=service.ObjectSpace.EnsureObject<SpeechLanguage>(language => language.Name==voiceInfo.Locale,language => language.Name=voiceInfo.Locale,inTransaction:true);
            speechVoice.Name = voiceInfo.LocalName;
            speechVoice.ShortName = voiceInfo.ShortName;
            speechVoice.VoicePath = voiceInfo.VoicePath;
            speechVoice.VoiceType = voiceInfo.VoiceType;
            speechVoice.Service=service;
        }

        private static IObservable<Unit> UpdateVoicesOnCommit(this ApplicationModulesManager manager) 
            => manager.WhenSpeechApplication(application => application.WhenCommitted<BusinessObjects.SpeechService>(ObjectModification.New).ToObjects()
                    .SelectMany(UpdateVoices))
                .ToUnit();

        private static IObservable<VoiceInfo[]> UpdateVoices(this BusinessObjects.SpeechService service) 
            => Observable.Using(() => new SpeechSynthesizer(service.SpeechConfig()),synthesizer => synthesizer.GetVoicesAsync().ToObservable()
                .ObserveOnContext().SelectMany(result => !string.IsNullOrEmpty(result.ErrorDetails)?Observable.Throw<SynthesisVoicesResult>(new SpeechException(result.ErrorDetails)):result.ReturnObservable()))
                .SelectMany(result => result.Voices.ToNowObservable()
                    .Do(service.EnsureVoice ).BufferUntilCompleted().Do(_ => {
                        service.CommitChanges();
                        service.ObjectSpace.Refresh();
                    }));
    }
}