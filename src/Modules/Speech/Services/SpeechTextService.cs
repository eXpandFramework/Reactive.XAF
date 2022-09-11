using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Xpand.XAF.Modules.Speech.BusinessObjects;

namespace Xpand.XAF.Modules.Speech.Services {
    public static class SpeechTextService {
        public static SpeechLanguage Language(this SpeechText speechText)
            => speechText is SpeechTranslation translation?translation.Language:speechText.SpeechToText.RecognitionLanguage;

        public static SpeechVoice SpeechVoice(this SpeechText speechText) {
            var speechLanguage = speechText.Language();
            var voices =speechText is SpeechTranslation?speechText.SpeechToText.SpeechVoices: speechText.SpeechToText.Account.Voices;
            return voices.FirstOrDefault(voice => voice.Language.Name == speechLanguage.Name);
        }

        private static SpeechConfig SpeechSynthesisConfig(this SpeechText speechText) {
            var speechConfig = speechText.SpeechToText.Account.SpeechConfig();
            speechConfig.SpeechSynthesisLanguage = speechText is SpeechTranslation translation
                ? translation.Language.Name : speechText.SpeechToText.RecognitionLanguage.Name;
            speechConfig.SpeechSynthesisVoiceName = $"{speechText.SpeechVoice()?.ShortName}";
            return speechConfig;
        }

        public static IObservable<SpeechSynthesisResult> SayIt(this SpeechText speechText) 
            => Observable.Using(() => new SpeechSynthesizer(speechText.SpeechSynthesisConfig()),synthesizer 
                => synthesizer.SpeakTextAsync(speechText.Text).ToObservable().Select(result => result));
    }
}