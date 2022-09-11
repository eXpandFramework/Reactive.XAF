using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using Microsoft.CognitiveServices.Speech;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Speech.BusinessObjects;

namespace Xpand.XAF.Modules.Speech.Services {
    public static class AccountService {
        public static IObservable<Unit> ConnectAccount(this ApplicationModulesManager manager) {
            return manager.UpdateAccounts();
        }

        public static SpeechAccount DefaultAccount(this IObjectSpace space,XafApplication application) 
            => space.FindObject<SpeechAccount>(CriteriaOperator.Parse(application.Model.SpeechModel().DefaultAccountCriteria));

        public static IObservable<Unit> Speak(this SpeechAccount defaultAccount, IModelSpeech speechModel) 
            => Observable.Using(() => new SpeechSynthesizer(defaultAccount.SpeechConfig()), synthesizer
                => synthesizer.SpeakSsmlAsync(Clipboard.GetText()).ToObservable().ObserveOnContext()
                    .DoWhen(_ => !new DirectoryInfo(speechModel.DefaultStorageFolder).Exists,
                        _ => Directory.CreateDirectory(speechModel.DefaultStorageFolder))
                    .SelectMany(result => {
                        var lastSpeak = defaultAccount.ObjectSpace.GetObjectsQuery<TextToSpeech>().Max(speech => speech.Oid) + 1;
                        var path = $"{speechModel.DefaultStorageFolder}\\{lastSpeak}.wav";
                        return File.WriteAllBytesAsync(path, result.AudioData).ToObservable().ObserveOnContext()
                            .SelectMany(_ => {
                                var textToSpeech = defaultAccount.ObjectSpace.CreateObject<TextToSpeech>();
                                textToSpeech.Duration = result.AudioDuration;
                                textToSpeech.File = textToSpeech.CreateObject<FileLinkObject>();
                                textToSpeech.File.FileName = Path.GetFileName(path);
                                textToSpeech.File.FullName = path;
                                return textToSpeech.Commit();
                            });
                    }));

        public static SpeechConfig SpeechConfig(this SpeechAccount account,SpeechLanguage recognitionLanguage=null,[CallerMemberName]string callerMember="") {
            var speechConfig = Microsoft.CognitiveServices.Speech.SpeechConfig.FromSubscription(account.Subscription, account.Region);
            speechConfig.SpeechRecognitionLanguage = $"{recognitionLanguage?.Name}";
            speechConfig.EnableAudioLogging();
            var path = $"{AppDomain.CurrentDomain.ApplicationPath()}\\Logs\\{nameof(SpeechConfig)}{callerMember}.log";
            if (!File.Exists(path)) {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            }
            speechConfig.SetProperty(PropertyId.Speech_LogFilename, path);
            return speechConfig;
        }

        public static SpeechTranslationConfig TranslationConfig(this SpeechAccount account) 
            => SpeechTranslationConfig.FromSubscription(account.Subscription, account.Region);
        
        private static void NewVoice(this SpeechAccount account,VoiceInfo voiceInfo) {
            var speechVoice = account.ObjectSpace.EnsureObject<SpeechVoice>(voice => voice.Account!=null&& voice.Account.Oid==account.Oid&&voice.Name==voiceInfo.Name,inTransaction:true);
            speechVoice.Gender = voiceInfo.Gender;
            speechVoice.Language=account.ObjectSpace.EnsureObject<SpeechLanguage>(language => language.Name==voiceInfo.Locale,language => language.Name=voiceInfo.Locale,true);
            speechVoice.Name = voiceInfo.LocalName;
            speechVoice.ShortName = voiceInfo.ShortName;
            speechVoice.VoicePath = voiceInfo.VoicePath;
            speechVoice.VoiceType = voiceInfo.VoiceType;
            speechVoice.Account=account;
        }

        private static IObservable<Unit> UpdateAccounts(this ApplicationModulesManager manager) 
            => manager.WhenSpeechApplication(application => application.WhenCommitted<SpeechAccount>(ObjectModification.New)
                    .ToObjects().Select(account => (account,SynchronizationContext.Current))
                    .SelectMany(t => Observable.Using(() => new SpeechSynthesizer(t.account.SpeechConfig()),synthesizer => synthesizer.GetVoicesAsync().ToObservable()
                        .ObserveOnContext(t.Current).SelectMany(result => result.Voices.ToNowObservable()
                            .Do(info =>t.account.NewVoice(info) ).BufferUntilCompleted().Do(_ => t.account.CommitChanges())))))
                .ToUnit();


    }
}