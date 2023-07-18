using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using Swordfish.NET.Collections.Auxiliary;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.Reactive.Transform.System.Net;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Speech.BusinessObjects;

namespace Xpand.XAF.Modules.Speech.Services {
    public static class TranslationService {
        public static SingleChoiceAction Translate(this (SpeechModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(Translate)).Cast<SingleChoiceAction>();
        
        internal static IObservable<Unit> Translate(this ApplicationModulesManager manager)
            => manager.RegisterViewSingleChoiceAction(nameof(Translate), action => {
                    action.SetImage(CommonImage.Language);
                    action.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
                    action.ItemType=SingleChoiceActionItemType.ItemIsOperation;
                    action.TargetViewType=ViewType.ListView;
                    action.TargetObjectType = typeof(SpeechText);
                })
                .AddItems(action => action.Controller.Frame.AsNestedFrame().ViewItem.View.CurrentObject
                    .Cast<SpeechToText>().SpeechVoices.Select(voice => voice.Language.ShortName()).Distinct().ToNowObservable()
                    .Do(language => action.Items.Add(new ChoiceActionItem(language, language))).ToUnit() )
                .WhenConcatExecution(e => {
                    var targetLanguage = (string)e.SelectedChoiceActionItem.Data;
                    return e.SelectedObjects.Cast<SpeechText>().ToArray().ToNowObservable()
                        .SpeechSelectMany(text => text.Translate(  targetLanguage,text.ObjectSpace.DefaultSpeechKeyword(e.Application().Model.SpeechModel())))
                        .BufferUntilCompleted().ObserveOnContext().SelectMany(translations => translations.Do(translation => {
                            var sourceText = translation.text;
                            var speechTranslation = sourceText.ObjectSpace.CreateObject<SpeechTranslation>();
                            speechTranslation.SpeechToText = sourceText.SpeechToText;
                            speechTranslation.Language = sourceText.SpeechToText.TargetLanguages.First(language => language.ShortName() ==targetLanguage);
                            speechTranslation.SourceText=sourceText;
                            speechTranslation.Duration = sourceText.Duration;
                            speechTranslation.Start = sourceText.Start;
                            speechTranslation.Text = translation.translation;
                        }).ToNowObservable());
                })
                .ToUnit();

        private static IObservable<(SpeechText text, string targetLanguage, string translation)> Translate(
            this SpeechText text, string targetLanguage, SpeechKeyword speechKeyword) {
            var requestMessage = text.NewTranslateRequest(text.SpeechToText.Translator, targetLanguage,speechKeyword);
            return AppDomain.CurrentDomain.HttpClient()
                .Send<Dictionary<string, List<Dictionary<string, string>>>>(requestMessage)
                .Select(dictionary => dictionary["translations"][0]["text"])
                .Select(translation => Regex.Replace(translation, @"<span\b[^>]* class=""notranslate"">(.*?)</span>", "$1", RegexOptions.IgnoreCase | RegexOptions.Singleline))
                .Select(translation => (text, targetLanguage, translation));
        }
        
        private static HttpRequestMessage NewTranslateRequest(this SpeechText speechText, TranslatorService service, string language, SpeechKeyword speechKeyword) {
            var text = speechText.Text;
            speechKeyword.Text.Split(';').ForEach(key => text = Regex.Replace(text, $"({key})",
                    "<span class=\"notranslate\">$1</span>", RegexOptions.IgnoreCase | RegexOptions.Singleline));
            
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post,
                $"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from={speechText.Language().ShortName()}&to={language}") {
                Content = new StringContent(new[]{new { Text=text }}.Serialize(), Encoding.UTF8, "application/json")
            };
            httpRequestMessage.Headers.Add("Ocp-Apim-Subscription-Key", service.Key);
            httpRequestMessage.Headers.Add("Ocp-Apim-Subscription-Region", service.Region);
            httpRequestMessage.Headers.Add("X-ClientTraceId", Guid.NewGuid().ToString());
            return httpRequestMessage;
        }

        private static SpeechTranslationConfig TranslationConfig(this SpeechToText speechToText) {
            var translationConfig = speechToText.Speech.TranslationConfig();
            translationConfig.SpeechRecognitionLanguage = speechToText.RecognitionLanguage.Name;
            speechToText.TargetLanguages.ForEach(language => translationConfig.AddTargetLanguage(language.Name));
            return translationConfig;
        }

        internal static IObservable<Unit> Translate(this SpeechToText speechToText,AudioConfig audioConfig, SynchronizationContext context,SimpleAction simpleAction)
            =>Observable.Using(() => new TranslationRecognizer(speechToText.TranslationConfig(),audioConfig),recognizer => recognizer
                .StartContinuousRecognitionAsync().ToObservable().MergeIgnored(_ => recognizer.NotifyWhenCanceled()).TraceSpeechManager(_ => "Started")
                .SelectMany(_ => recognizer.WhenSessionStopped().TakeUntil(simpleAction.WhenExecuted().Where(e => e.Action.CommonImage()==CommonImage.Stop).Take(1)
                        .SelectMany(_ => recognizer.StopContinuousRecognitionAsync().ToObservable()))
                    .TakeFirst().ObserveOn(context).Do(_ => simpleAction.SetImage(CommonImage.Language))
                    .MergeToUnit(recognizer.Defer(() => {
                        var speechTexts = speechToText.SpeechTexts.OrderByDescending(text => text.Start).ToArray();
                        return recognizer.WhenRecognized().ObserveOn(context)
                            .SelectMany((e, _) => e.Result.Translations.ToNowObservable().TraceSpeechManager()
                                .Select(pair => speechToText.NewSpeechTranslation(simpleAction, pair, e, speechTexts))
                            ).ToUnit()
                            .IgnoreElements();
                    }))));
        private static SpeechTranslation NewSpeechTranslation(this SpeechToText speechToText, KeyValuePair<string, string> pair, TranslationRecognitionResult result, SpeechText[] speechTexts) {
            var speechTranslation = speechToText.NewSpeechText<SpeechTranslation>(pair.Value, result.Duration, result.OffsetInTicks,
                speechToText.TargetLanguages.FirstOrDefault(language => language.Name.Split('-')[0] == pair.Key));
            var sourceText = speechTexts.Last(text => text.Text.CalculateSimilarity(result.Text)>0.75);
            speechTranslation.SourceText=sourceText;
            sourceText.RealTranslations.Add(speechTranslation);
            sourceText.Translations.Add(speechTranslation);
            speechTranslation.CommitChanges();
            return speechTranslation;
        }

        private static Frame NewSpeechTranslation(this SpeechToText speechToText, SimpleAction simpleAction,
            KeyValuePair<string, string> pair, TranslationRecognitionEventArgs e, SpeechText[] speechTexts) {
            var speechTextFrame = simpleAction.View().ToDetailView().FrameContainers(typeof(SpeechTranslation)).First();
            var newSpeechTranslation = speechToText.NewSpeechTranslation(pair, e.Result, speechTexts);
            var collectionSource = speechTextFrame.View.ToListView().CollectionSource;
            collectionSource.Add(collectionSource.ObjectSpace.GetObject(newSpeechTranslation));
            return speechTextFrame;
        }

    }
}