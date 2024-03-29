﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using NAudio.Wave;
using Xpand.Extensions.DateTimeExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.Speech.BusinessObjects;

namespace Xpand.XAF.Modules.Speech.Services {
    public static class SpeechTextService {

        internal static TimeSpan Tolerance(this SpeechText current) => TimeSpan.FromMilliseconds(100);

        internal static string GetRateTag(this SpeechText current, int i) {
            if (current.FileDuration.HasValue ) {
                if (!current.CanConvert) {
                    var maxTime = current.Duration.Add(current.SpareTime);
                    if (current.FileDuration.Value > maxTime) {
                        var rate = current.FileDuration.Value.PercentageDifference(maxTime)+i;
                        return @$"<prosody rate=""+{rate}%"">{{0}}</prosody>";    
                    }
                }
                else if(current.Rate>0) {
                    return @$"<prosody rate=""+{current.Rate}%"">{{0}}</prosody>";
                }
            }
            

            return null;
        }

        internal static IEnumerable<string> Breaks(this SpeechText current, SpeechText previous) {
            var waitTime = current.WaitTime( );
	        
            if (waitTime<TimeSpan.Zero) {
                // throw new SpeechException($"Negative break after: {previous.Text}");
            }
	        
            // int breakLimit = 5;
            // if (previous.VoiceDuration>previous.Duration) {
            //  waitTime -= previous.VoiceOverTime();
            //  if (waitTime<TimeSpan.Zero) {
            //   return Enumerable.Empty<string>();
            //  }
            // }

            return current.Text.YieldItem();
            // if (waitTime.TotalSeconds > breakLimit) {
            //  var roundedSeconds =waitTime.TotalSeconds>breakLimit? (waitTime.TotalSeconds % breakLimit).Round(2):0;
            //  return Enumerable.Range(0, (int)(waitTime.TotalSeconds / breakLimit))
            //   .Select(_ => $"<break time=\"{breakLimit}s\" />")
            //   .Concat($"<break time=\"{roundedSeconds}s\" />{current.Text}");
            // }
            //
            // if (waitTime.TotalSeconds > 0) {
            //  return $"<break time=\"{waitTime.TotalSeconds}s\" />{current.Text}".YieldItem();
            // }
            // else
            //  return current.Text.YieldItem();
        }

        internal static TimeSpan VoiceOverTime(this SpeechText speechText) 
            => speechText.VoiceDuration > speechText.Duration ? speechText.VoiceDuration.Value.Subtract(speechText.Duration) : TimeSpan.Zero;

        internal static TimeSpan SpareTime(this SpeechText current) {
            var nextSpeechText = current.NextSpeechText();
            return nextSpeechText == null ? TimeSpan.Zero : nextSpeechText.Start.Subtract(current.End).Abs();
        }

        internal static TimeSpan WaitTime(this SpeechText current) {
            var previous = current.PreviousSpeechText();
            return previous == null ? TimeSpan.Zero : current.Start.Subtract(previous.End);
        }
        
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        public static (ISampleProvider provider, SpeechText speechText)[] AudioProviders(this SpeechText speechText) {
            var reader = new AudioFileReader(speechText.File.FullName);
            var nextSpeechText = speechText.NextSpeechText();
            if (nextSpeechText==null|| reader.TotalTime <= speechText.Duration.Add(speechText.SpareTime).Add(speechText.Tolerance())) {
                var nextStart = nextSpeechText?.Start??TimeSpan.Zero;
                var waitTime = nextStart.Subtract(speechText.Start.Add(reader.TotalTime)) ;
                 if (waitTime > TimeSpan.Zero) {
                    return new[] { reader,new SilenceProvider(reader.WaveFormat).ToSampleProvider().Take(waitTime) }
                        .Select(provider => (provider, speechText)).ToArray();    
                }
                return new[] { reader }.Cast<ISampleProvider>().Select(provider => (provider, speechText)).ToArray();
            }
            throw new NotImplementedException();
        }

        public static string WavFileName(this IAudioFileLink audioFileLink,IModelSpeech model,Func<long,string> fileName=null) {
            var storage = audioFileLink?.Storage;
            fileName ??= s => s.ToString();
            storage ??= model.DefaultStorageFolder;
            return $"{ storage}\\{fileName(audioFileLink?.Oid??0)}.wav";
        }

        public static IObservable<SpeechText> SaveSSMLFile(this SpeechText speechText, string fileName, SpeechSynthesisResult result) 
            => File.WriteAllBytesAsync(fileName, result.AudioData).ToObservable()
                .BufferUntilCompleted().ObserveOnContext()
                .Select(_ => speechText.UpdateSSMLFile(result, fileName))
                .TakeFirst();
        public static SpeechLanguage Language(this SpeechText speechText)
            => speechText is SpeechTranslation translation?translation.Language:speechText.SpeechToText.RecognitionLanguage;

        public static SpeechVoice SpeechVoice(this SpeechText speechText) {
            var speechLanguage = speechText.Language();
            var voices =speechText is SpeechTranslation?speechText.SpeechToText.SpeechVoices: speechText.SpeechToText.Speech.Voices;
            return voices.FirstOrDefault(voice => voice.Language.Name == speechLanguage.Name);
        }

        private static SpeechConfig SpeechSynthesisConfig(this SpeechText speechText) {
            var speechConfig = speechText.SpeechToText.Speech.SpeechConfig();
            speechConfig.SpeechSynthesisLanguage = speechText is SpeechTranslation translation
                ? translation.Language.Name : speechText.SpeechToText.RecognitionLanguage.Name;
            speechConfig.SpeechSynthesisVoiceName = $"{speechText.SpeechVoice()?.ShortName}";
            return speechConfig;
        }

        // public static IObservable<SpeechSynthesisResult> SayIt(this SpeechText speechText) 
        //     => Observable.Using(() => new SpeechSynthesizer(speechText.SpeechSynthesisConfig()),synthesizer 
        //         => synthesizer.SpeakTextAsync(speechText.Text).ToObservable().Select(result => result));
    }
}