using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Swordfish.NET.Collections.Auxiliary;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.Extensions.XAF.Xpo.Xpo;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Speech.BusinessObjects;

namespace Xpand.XAF.Modules.Speech.Services {
	public static class SpeechToTextService {
		public static ParametrizedAction SpareTime(this (SpeechModule, Frame frame) tuple)
			=> tuple.frame.ParametrizedAction(nameof(SpareTime));
        public static SimpleAction SpeechToText(this (SpeechModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(SpeechToText)).To<SimpleAction>();
        
        public static ParametrizedAction Rate(this (SpeechModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(Rate)).To<ParametrizedAction>();
        public static SimpleAction Translate(this (SpeechModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(Translate)).To<SimpleAction>();
		
        public static SingleChoiceAction Synthesize(this (SpeechModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(Synthesize)).To<SingleChoiceAction>();
        
        public static IObservable<Unit> ConnectSpeechToText(this ApplicationModulesManager manager) 
	        => manager.SpeechToTextAction(nameof(SpeechToText), CommonImage.ConvertTo, Recognize())
		        .Merge(manager.SpeechToTextAction(nameof(Translate), CommonImage.Language, Translate()))
		        .Merge(manager.Synthesize()).Merge(manager.SSML())
		        .Merge(manager.ConfigureSpeechTextView())
		        .Merge(manager.SynthesizeOnSave())
		        .Merge(manager.SpareTime())
		        .Merge(manager.Rate())
		        .Merge(manager.NewSpeechTextFromUI());

        private static IObservable<Unit> SynthesizeOnSave(this ApplicationModulesManager manager)
			=> manager.WhenSpeechApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechToText))
				.Where(frame => frame.View.ObjectTypeInfo.Type==typeof(SpeechToText))
				.SelectUntilViewClosed(frame => {
					var observeOnContext = frame.View.ObjectSpace
						.WhenCommittedDetailed<SpeechText>(ObjectModification.NewOrUpdated, text => text.Text != null, nameof(SpeechText.Text))
						.Select(t => t).Where(t => t.details.Any()).ToObjects()
						// .WaitUntilInactive(2)
						.ObserveOnContext();
					return observeOnContext
						.SelectMany(speechText => frame.View.ToDetailView().FrameContainers(typeof(SpeechText))
							// .Where(speechTextFrame => speechTextFrame.View.ObjectTypeInfo.Type == text.GetType())
							.SelectMany(speechTextFrame => speechTextFrame
								.Actions<SingleChoiceAction>(nameof(Synthesize))
								.Do(action => {
									speechText.YieldItem().ToArray().UpdateSSML(frame);
									action.DoExecute(action.Items.First(item => (string)item.Data == "SSML"),speechText);
								})));
				}))
				.ToUnit();
        
        // public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)>
	       //  WhenCommittedDetailed1<T>(this IObjectSpace objectSpace, ObjectModification objectModification,Func<T, bool> criteria=null,params string[] modifiedProperties) 
	       //  => objectSpace.WhenCommitingDetailed1(true, objectModification,criteria, modifiedProperties);

        // public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)>
	       //  WhenCommitingDetailed1<T>(this IObjectSpace objectSpace, bool emitAfterCommit, ObjectModification objectModification,Func<T, bool> criteria,params string[] modifiedProperties) 
	       //  => modifiedProperties.Any()?objectSpace.WhenModifiedObjects1(typeof(T),modifiedProperties).Cast<T>().Take(1)
			     //    .SelectMany(_ => objectSpace.WhenCommitingDetailed(emitAfterCommit,objectModification, criteria,modifiedProperties)
				    //     .Select(t => t)):
		      //   objectSpace.WhenCommitingDetailed(objectModification, emitAfterCommit,criteria);

        // public static IObservable<object> WhenModifiedObjects1(this IObjectSpace objectSpace,Type objectType, params string[] properties) 
	       //  => Observable.Defer(() => objectSpace.WhenObjectChanged().Where(t => objectType.IsInstanceOfType(t.e.Object) && properties.PropertiesMatch( t))
			     //    .Select(_ => _.e.Object).Take(1))
		      //   .RepeatWhen(observable => observable.SelectMany(_ => objectSpace.WhenModifyChanged().Where(space => !space.IsModified).Take(1)))
		      //   .TakeUntil(objectSpace.WhenDisposed());
        
        // private static bool PropertiesMatch(this string[] properties, (IObjectSpace objectSpace, ObjectChangedEventArgs e) t) 
	       //  => !properties.Any()||(t.e.MemberInfo != null && properties.Contains(t.e.MemberInfo.Name) ||
	       //                         t.e.PropertyName != null && properties.Contains(t.e.PropertyName));

        
        
        private static IObservable<Unit> ConfigureSpeechTextView(this ApplicationModulesManager manager)
	        => manager.WhenSpeechApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechToText), ViewType.DetailView)
			        .SelectUntilViewClosed(frame => frame.View.ToDetailView().NestedFrameContainers(typeof(SpeechText))
				        .Do(container => ((XPObjectSpace)container.Frame.View.ObjectSpace).PopulateAdditionalObjectSpaces(application)))
			        .MergeToUnit(application.SynchronizeSpeechTextListViewSelection()))
		        .MergeToUnit(manager.ConfigureSpeechTranslationView());
        
        private static IObservable<Unit> ConfigureSpeechTranslationView(this ApplicationModulesManager manager)
	        => manager.WhenSpeechApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechToText), ViewType.DetailView)
			        .SelectUntilViewClosed(frame => frame.View.ToDetailView().NestedFrameContainers(typeof(SpeechTranslation))
				        .Do(container => container.Frame.View.ToListView().CollectionSource
					        .SetCriteria<SpeechTranslation>(translation => translation.SpeechToText.Oid== ((SpeechToText)frame.View.CurrentObject).Oid)))
		        ).ToUnit()
		        .MergeToUnit(manager.WhenSpeechApplication(application => application.WhenDetailViewCreating(typeof(SpeechToText))
			        .SelectUntilViewClosed(_ => application.WhenListViewCreating(typeof(SpeechTranslation)).Where(t1 => t1.e.ObjectSpace is not INestedObjectSpace)
				        .Do(_ => {
					        // var nestedObjectSpace = _.e.ObjectSpace.CreateNestedObjectSpace();
					        // t1.e.View = (ListView)application.NewView(application.Model.Views[t1.e.ViewID], nestedObjectSpace);
				        }))
		        ))
		        .MergeToUnit(manager.WhenSpeechApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechToText),ViewType.DetailView)
			        .SelectUntilViewClosed(frame => frame.View.ObjectSpace.WhenCommitted<SpeechText>(ObjectModification.Deleted)
				        .Do(_ => frame.GetController<RefreshController>().RefreshAction.DoExecute()))));


        private static IObservable<Unit> SynchronizeSpeechTextListViewSelection(this XafApplication application) {
	        return application.WhenNestedListViewsSelectionChanged<SpeechText, SpeechTranslation>(
			        (speechText, speechTranslation) => speechText.Start == speechTranslation.SourceText?.Start, 
			        sourceOrderSelector: text => text.Start,targetOrderSelector:translation => translation.Start)
		        .SynchronizeGridListEditor().ToUnit();
        }


        private static IObservable<Unit> SSML(this ApplicationModulesManager manager) 
	        => manager.WhenSpeechApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechText),ViewType.ListView)
			        .SelectUntilViewClosed(frame => frame.View.WhenSelectionChanged(1)
				        .Do(view => view.AsListView().CollectionSource.Objects<SpeechText>().FirstOrDefault()?.SpeechToText.TranslationSSMLs.Clear())
				        .Do(view => view.SelectedObjects.Cast<SpeechText>().OrderBy(text => text.Start).ToArray()
					        .UpdateSSML(frame))))
		        .ToUnit();

        private static void UpdateSSML(this SpeechText[] source,Frame frame) {
	        var rate = frame.ParametrizedAction(nameof(Rate))?.Value??0;
	        source.GroupBy(speechText => speechText.Language())
		        .WhereNotDefault(speechTexts => speechTexts.Key)
		        .ForEach(speechTexts => speechTexts.UpdateSSML((speechText, texts) => frame.Application.SSMLText(speechText, texts, rate)));
        }
        private static void EnsureStorage(this SpeechToText speechToText, XafApplication xafApplication) {
	        if (speechToText.Storage.IsNullOrEmpty()) {
		        // speechToText.Storage = xafApplication.Model.SpeechModel().DefaultStorageFolder;
	        }
        }

        private static IObservable<Unit> NewSpeechTextFromUI(this ApplicationModulesManager manager) 
	        => manager.WhenSpeechApplication(application => application.WhenViewOnFrame(typeof(SpeechText),ViewType.ListView)
			        .SelectUntilViewClosed(frame => frame.GetController<NewObjectViewController>().WhenEvent<ObjectCreatedEventArgs>(nameof(NewObjectViewController.ObjectCreated))
				        .Do(e => {
					        var speechText = ((SpeechText)e.CreatedObject);
					        if (speechText.IsNewObject) {
						        speechText.SpeechToText = frame.ParentObject<SpeechToText>();
						        speechText.SpeechToText.EnsureStorage(application);
						        speechText.SetMemberValue("_oid", 1+(speechText.SpeechToText.SpeechTexts.Where(text => text.Oid > 0)
								        .MaxBy(text => text.Start)?.Oid ?? 0));
						        var previousSpeechText = speechText.PreviousSpeechText();
						        speechText.Start = previousSpeechText?.Start.Add(previousSpeechText.Duration)??TimeSpan.Zero;
						        speechText.Start+=TimeSpan.FromSeconds(1);
					        }
					        
				        })))
		        .ToUnit();
        
        private static string SSMLText(this XafApplication application, SpeechText speechText, SpeechText[] speechTexts, object rate) {
	        var speechModel = application.Model.SpeechModel();
	        var speechVoice = speechText.SpeechVoice();
	        var firstText = speechVoice.SSMLText( speechText.Text,speechModel,speechText.GetRateTag((int)(rate??0)));
	        var voiceText = speechTexts.ToNowObservable().CombineWithPrevious().WhenNotDefault(t => t.previous)
		        .ToEnumerable().ToArray()
		        .Select(t => (ssml:t.current.Breaks(t.previous).Join(""),rate:t.current.GetRateTag((int)(rate ?? 0))))
		        .Select(t => speechVoice.SSMLText(t.ssml,speechModel, t.rate)).Join("");

	        return $"{firstText}{voiceText}";
        }

        private static string SSMLText(this SpeechVoice speechVoice,string ssml, IModelSpeech speechModel, string rate=null) 
	        =>ssml==null?null: speechModel.SSMLSpeakFormat.StringFormat(
		        speechModel.SSMLVoiceFormat.StringFormat(speechVoice?.ShortName, rate.StringFormat($"{ssml}")));
        
        

        private static void UpdateSSML(this IGrouping<SpeechLanguage, SpeechText> speechTexts, Func<SpeechText, SpeechText[], string> ssmlText) {
	        var speechText = speechTexts.First();
	        var array = speechTexts.ToArray();
	        var text = ssmlText(speechText,array);
	        if (!text.IsNullOrEmpty()) {
		        var additionalObjectSpace = speechText.ObjectSpace.AdditionalObjectSpace(typeof(SSML));
		        var ssml = additionalObjectSpace.CreateObject<SSML>();
		        ssml.Language = speechTexts.Key;
		        ssml.SpeechTexts.AddRange(array.Select(text1 => text1));
		        ssml.Text = text;
		        if (speechText.GetType() == typeof(SpeechTranslation)) {
			        speechText.SpeechToText.TranslationSSMLs.Add(ssml);
		        }
		        else {
			        speechText.SpeechToText.SSML = ssml;
		        }
		        ssml.RemoveFromModifiedObjects();
	        }
        }

        private static Func<AudioConfig, SpeechToText, SynchronizationContext, SimpleAction, IObservable<Unit>> Recognize() 
	        => (audioConfig, speechToText, context, action) =>speechToText.Recognize( audioConfig, context, action);
        
        private static IObservable<Unit> Recognize(this SpeechToText speechToText, AudioConfig audioConfig, SynchronizationContext context, SimpleAction simpleAction)
	        => Observable.Using(() => new SpeechRecognizer(speechToText.Account.SpeechConfig(speechToText.RecognitionLanguage), audioConfig),recognizer => recognizer.StartContinuousRecognitionAsync().ToObservable()
		        .SelectMany(_ => recognizer.WhenSessionStopped().TakeUntil(simpleAction.WhenExecuted().Where(e => e.Action.CommonImage()==CommonImage.Stop).Take(1)
				        .SelectMany(_ => recognizer.StopContinuousRecognitionAsync().ToObservable()))
			        .FirstAsync().ObserveOn(context).Do(_ => simpleAction.SetImage(CommonImage.ConvertTo))
			        .MergeToUnit(recognizer.WhenRecognized().ObserveOn(context)
				        .Do(result => speechToText.NewSpeechText<SpeechText>(result.Text, result.Duration, result.OffsetInTicks,simpleAction.Application))
				        .IgnoreElements())));
        
        private static T NewSpeechText<T>(this SpeechToText speechToText, string text,TimeSpan duration, long offset,XafApplication application,SpeechLanguage speechLanguage=null) where T:SpeechText{
	        var speechText = speechToText.ObjectSpace.CreateObject<T>();
	        speechText.SpeechToText = speechToText;
	        speechText.SpeechToText.EnsureStorage(application);
	        speechToText.SpeechTexts.Add(speechText);
	        speechText.Text = text;
	        speechText.Start=TimeSpan.FromTicks(offset);
	        speechText.Duration = duration;
	        if (speechText is SpeechTranslation translation) {
		        translation.Language=speechLanguage;
	        }
	        // speechText.CommitChanges();
	        speechToText.FireChanged(nameof(BusinessObjects.SpeechToText.SpeechTexts));
	        return speechText;
        }
        
        private static SpeechTranslation NewSpeechTranslation(this SpeechToText speechToText, KeyValuePair<string, string> pair, TranslationRecognitionResult result, SpeechText[] speechTexts,XafApplication application) {
	        var speechTranslation = speechToText.NewSpeechText<SpeechTranslation>(pair.Value, result.Duration, result.OffsetInTicks,application,
		        speechToText.TargetLanguages.FirstOrDefault(language => language.Name.Split('-')[0] == pair.Key));
	        var sourceText = speechTexts.Last(text => text.Text.CalculateSimilarity(result.Text)>0.75);
	        speechTranslation.SourceText=sourceText;
	        sourceText.RealTranslations.Add(speechTranslation);
	        sourceText.Translations.Add(speechTranslation);
	        speechTranslation.CommitChanges();
	        return speechTranslation;
        }
        
        private static AudioConfig AudioConfig(this SpeechToText speechToText) {
	        if (speechToText is FileSpeechToText fileSpeechToText) {
		        return Microsoft.CognitiveServices.Speech.Audio.AudioConfig.FromWavFileInput(fileSpeechToText.File.FullName);	
	        }
	        throw new NotImplementedException();	
        }

        private static SpeechTranslationConfig TranslationConfig(this SpeechToText speechToText) {
	        var translationConfig = speechToText.Account.TranslationConfig();
	        translationConfig.SpeechRecognitionLanguage = speechToText.RecognitionLanguage.Name;
	        speechToText.TargetLanguages.ForEach(language => translationConfig.AddTargetLanguage(language.Name));
	        return translationConfig;
        }

        private static IObservable<Unit> Translate(this SpeechToText speechToText,AudioConfig audioConfig, SynchronizationContext context,SimpleAction simpleAction)
	        =>Observable.Using(() => new TranslationRecognizer(speechToText.TranslationConfig(),audioConfig),recognizer => recognizer
		        .StartContinuousRecognitionAsync().ToObservable().MergeIgnored(_ => recognizer.NotifyWhenCanceled()).TraceSpeechManager(_ => "Started")
		        .SelectMany(_ => recognizer.WhenSessionStopped().TakeUntil(simpleAction.WhenExecuted().Where(e => e.Action.CommonImage()==CommonImage.Stop).Take(1)
				        .SelectMany(_ => recognizer.StopContinuousRecognitionAsync().ToObservable()))
			        .FirstAsync().ObserveOn(context).Do(_ => simpleAction.SetImage(CommonImage.Language))
			        .MergeToUnit(recognizer.Defer(() => {
				        var speechTexts = speechToText.SpeechTexts.OrderByDescending(text => text.Start).ToArray();
				        return recognizer.WhenRecognized().ObserveOn(context)
					        .SelectMany((e, _) => e.Result.Translations.ToNowObservable().TraceSpeechManager()
						        .Select(pair => speechToText.NewSpeechTranslation(simpleAction, pair, e, speechTexts))
					        ).ToUnit()
					        .IgnoreElements();
			        }))));

        private static Frame NewSpeechTranslation(this SpeechToText speechToText, SimpleAction simpleAction,
	        KeyValuePair<string, string> pair, TranslationRecognitionEventArgs e, SpeechText[] speechTexts) {
	        var speechTextFrame = simpleAction.View().ToDetailView().FrameContainers(typeof(SpeechTranslation)).First();
	        var newSpeechTranslation = speechToText.NewSpeechTranslation(pair, e.Result, speechTexts,simpleAction.Application);
	        var collectionSource = speechTextFrame.View.ToListView().CollectionSource;
	        collectionSource.Add(collectionSource.ObjectSpace.GetObject(newSpeechTranslation));
	        return speechTextFrame;
        }

        private static Func<AudioConfig, SpeechToText, SynchronizationContext, SimpleAction, IObservable<Unit>> Translate() 
	        => (audioConfig, speechToText, context, action) =>speechToText.Translate( audioConfig, context, action);

        private static IObservable<Unit> Rate(this ApplicationModulesManager manager)
	        => manager.RegisterViewParametrizedAction(nameof(Rate),typeof(int), action => action.TargetObjectType = typeof(SpeechToTextService))
		        .WhenConcatExecution(e => e.Action.Frame().AsNestedFrame().ViewItem.View.Refresh())
		        .ToUnit();
        private static IObservable<Unit> SpareTime(this ApplicationModulesManager manager)
	        => manager.RegisterViewParametrizedAction(nameof(SpareTime),typeof(int), action => action.TargetObjectType = typeof(SpeechText))
		        .WhenConcatExecution(e => {
			        var speechTexts = e.Action.SelectedObjects<SpeechText>().OrderBy(text => text.Start).ToArray();
			        var timeSpan = e.Action.AsParametrizedAction().Value.Change<int>();
			        return speechTexts.ToNowObservable()
				        .SelectMany(current => {
					        if (timeSpan == 0) {
						        current.Start = current.Previous?.End??TimeSpan.Zero;
						        return Unit.Default.ReturnObservable();
					        }
					        if (timeSpan > 0) {
						        return Observable.While(() => current.Next != null&&speechTexts.Contains(current.Next), current.Defer(() => {
							        current.Next.Start += TimeSpan.FromSeconds(timeSpan);
							        current = current.Next;
						        }));
					        }

					        return Observable.Empty<Unit>();
				        });
		        })
		        .ToUnit();
        
        private static IObservable<SSMLFile> Join(this IEnumerable<SpeechText> source,IModelSpeech modelSpeech) 
	        => source.ToArray().ToNowObservable()
		        .SpeechSelectMany(speechText => speechText.AudioProviders())
		        .BufferUntilCompleted().ObserveOnContext()
		        .SpeechSelectMany(providers => {
			        var filename = $"{modelSpeech.DefaultStorageFolder}\\{providers.First().speechText.File.Oid}_{providers.Last(t => t.speechText != null).speechText.File.Oid}.wav";
			        return providers.ConcatAudio(filename).ToArray().NewSSMLFile(filename);
		        });

        private static SpeechText[] ConcatAudio(this (ISampleProvider provider, SpeechText speechText)[] providers,string fileName) {
	        WaveFileWriter.CreateWaveFile16(fileName, new ConcatenatingSampleProvider(providers.Select(t => t.provider)));
	        return providers.Select(t => t.speechText).WhereNotDefault().DistinctBy(text => text.Oid).ToArray();
        }

        private static IObservable<Unit> Synthesize(this ApplicationModulesManager manager) 
            => manager.RegisterViewSingleChoiceAction(nameof(Synthesize), action => {
                    action.TargetObjectType = typeof(SpeechText);
                    action.TargetViewType=ViewType.ListView;
                    action.SelectionDependencyType=SelectionDependencyType.RequireMultipleObjects;
                    action.ItemType=SingleChoiceActionItemType.ItemIsOperation;
                    action.PaintStyle=ActionItemPaintStyle.Image;
                    action.DefaultItemMode=DefaultItemMode.LastExecutedItem;
                    action.SetImage(CommonImage.Start);
                    action.Shortcut = "CtrlL";
                    action.Items.Add(new ChoiceActionItem("SSML","SSML"));
                    action.Items.Add(new ChoiceActionItem("Text","Text"));
                    action.Items.Add(new ChoiceActionItem("Join","Join"));
                    action.Items.Add(new ChoiceActionItem("JoinTextSSML","JoinTextSSML"));
                },PredefinedCategory.ObjectsCreation)
                .ToUnit()
                .Merge(manager.WhenSpeechApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechToText),ViewType.DetailView)
                    .SelectUntilViewClosed(speechToTextFrame => speechToTextFrame.View.AsDetailView().NestedFrameContainers(typeof(SpeechText))
                        .SelectMany(editor => {
	                        var action = editor.Frame.SingleChoiceAction(nameof(Synthesize));
	                        return action.Synthesize(() => action.SelectedObjects<SpeechText>().ToArray());
                        })
                        .ToUnit())));

        private static IObservable<Unit> Synthesize(this SingleChoiceAction action, Func<SpeechText[]> source) 
	        => action.SpeakText(source).Merge(action.SpeakSSML(source))
		        .Merge(action.JoinAudio(source))
		        .Merge(action.JoinTextSSMLAudio());

        private static IObservable<Unit> JoinAudio(this SingleChoiceAction synthesizeAction,Func<SpeechText[]> speechTextSource) 
	        => synthesizeAction.WhenExecuted().Where(e => (string)e.SelectedChoiceActionItem.Data=="Join")
		        .SelectMany(e => speechTextSource().Join(e.Application().Model.SpeechModel())).ToUnit();

        private static IObservable<Unit> JoinTextSSMLAudio(this SingleChoiceAction synthesizeAction) 
	        => synthesizeAction.WhenExecuted().Where(e => (string)e.SelectedChoiceActionItem.Data=="JoinTextSSML")
		        .SelectMany(_ => Observable.Throw<Unit>(new NotImplementedException()));

        private static IObservable<Unit> StopSpeak(this SingleChoiceAction sayItAction,string data,Action<SpeechSynthesizer> configure=null,Func<SpeechSynthesizer,bool> filter=null) 
	        => sayItAction.WhenExecuted().Where(e => e.Action.CommonImage()==CommonImage.Stop&&(string)e.SelectedChoiceActionItem.Data==data)
		        .SelectMany(e => e.Action.View().ToListView().CollectionSource.Objects<SpeechText>().Select(text => text.SpeechVoice()).DistinctBy(voice => voice.Name).ToNowObservable()
			        .Select(voice => voice.SpeechSynthesizer(e.Action.View().ObjectTypeInfo.Type)).Do(synthesizer => configure?.Invoke(synthesizer)))
		        .Where(synthesizer => filter?.Invoke(synthesizer)??true)
		        .SelectMany(synthesizer => synthesizer.StopSpeakingAsync().ToObservable()).IgnoreElements();

        private static IEnumerable<SSML> SSMLs(this IEnumerable<SpeechText> speechTexts) {
	        var speechText = speechTexts.First();
	        return speechText is SpeechTranslation ? speechText.SpeechToText.TranslationSSMLs : speechText.SpeechToText.SSML.YieldItem();
        }

        private static SpeechVoice SpeechVoice(this SpeechToText speechToText,SpeechLanguage speechLanguage) 
	        => speechToText.SpeechVoices.FirstOrDefault(voice => voice.Language.Name == speechLanguage.Name)??speechToText.Account.Voices.First(voice => voice.Language.Name==speechLanguage.Name);
        
        static readonly Regex SpeakTagRegex = new(@"<speak\b[^>]*>(.*?)</speak>", RegexOptions.IgnoreCase | RegexOptions.Singleline|RegexOptions.Compiled);
        private static IObservable<SSMLFile> SpeakSSML(this ActionBase actionBase,Func<SpeechText[]> speechTextSourceSelector) {
	        var speechTextSource = speechTextSourceSelector();
	        return speechTextSource.SSMLs().ToNowObservable().WhenNotDefault()
		        .Select(ssml => {
			        var speechVoice = speechTextSource.First().SpeechToText.SpeechVoice(ssml.Language);
			        var speechTextType = actionBase.View().ObjectTypeInfo.Type;
			        return (ssml, speechSynthesizer: speechVoice.SpeechSynthesizer(speechTextType),
				        model: actionBase.Application.Model.SpeechModel());
		        })
		        .SelectManySequential(t => {
			        var ssmlText = t.ssml.Text;
			        var speechTexts = t.ssml.SpeechTexts;
			        return Observable.Defer(() => SpeakTagRegex
					        .SpeakSSML(ssmlText, t.speechSynthesizer, t.model, speechTexts, actionBase.Application)
					        .ShowXafMessage(actionBase.Application))
				        ;
		        })
		        .BufferUntilCompleted().ObserveOnContext()
		        .SelectMany(texts => texts.NewSSMLFile(actionBase.Application.Model.SpeechModel()));
        }

        private static IObservable<SpeechText> SpeakSSML(this Regex regex,string ssml,  SpeechSynthesizer speechSynthesizer, IModelSpeech model,List<SpeechText> speechTexts,XafApplication application) 
	        => regex.Matches(ssml).ToNowObservable()
		        .ShowXafMessage(application,match => match.Value)
		        .SelectManySequential((match,i) => {
			        var ssmlText = match.Value;
			        var speechText = speechTexts[i];
			        return Observable.Defer(() => speechSynthesizer.SpeakSSML(() => ssmlText).ObserveOnContext()
						        .Select(result => (result,speechText))
					        .SelectManySequential(t => speechText.SaveSSMLFile( speechText.WavFileName(model),t.result).To(t))
				        )
				        .RepeatWhen(observable => observable.ObserveOnContext()
					        .TakeUntil(_ => speechText.CanConvert).Where(_ => !speechText.CanConvert)
					        .Select(_ => {
						        var regexObj = new Regex(@"(<prosody rate=""\+)(?<rate>[^""]*)\b[^>]*>(.*?)(</prosody>)",
							        RegexOptions.IgnoreCase | RegexOptions.Singleline);
						        var rate = regexObj.Match(ssmlText).Groups["rate"].Value.Change<decimal>() + 5 * (i + 1);
						        if (ssmlText.Contains("<prosody rate=")) {
							        ssmlText = Regex.Replace(ssmlText, @"(<prosody rate="")\+(?<rate>[^""]*)\b[^>]*>(.*?)(</prosody>)",
								        $"$1+{rate}%\">$2$3", RegexOptions.IgnoreCase | RegexOptions.Singleline);    
						        }
						        else {
							        ssmlText = Regex.Replace(ssmlText, @"(<voice\b[^>]*>)(.*?)</voice>",
								        $"$1{speechText.GetRateTag((int)rate).StringFormat("$2")}</voice>",
								        RegexOptions.IgnoreCase | RegexOptions.Singleline);
						        }

						        speechText.Rate = rate;
						        return rate;
					        })
					        .ShowXafMessage(application));
		        })
		        .BufferUntilCompleted().WhenNotEmpty().ObserveOnContext().SelectMany()
		        .Select(t => t.speechText)
		        // .SelectMany(t => t.speechText.SaveSSMLFile(t.speechText.WavFileName(model), t.result).To(t.speechText))
	        ;

        private static IObservable<SpeechSynthesisResult> SpeakSSML(this SpeechSynthesizer speechSynthesizer, Func<string> ssml) 
	        => speechSynthesizer.Defer(() => speechSynthesizer.WhenSynthesisCompleted().Publish(whenSynthesisCompleted => 
			        speechSynthesizer.Defer(() => Observable.FromAsync(() => speechSynthesizer.SpeakSsmlAsync(ssml())).Select(result => result)
				        .Merge( Observable.Defer(() => speechSynthesizer.NotifyWhenSynthesisCanceled().TakeUntil(whenSynthesisCompleted.Select(c=>c))))
				        .Zip(whenSynthesisCompleted)
				        .FirstAsync().Select(t1 => t1.First))
		        ))
		        .RetryWithBackoff(3).FirstAsync();
        
        
        static IObservable<SSMLFile> NewSSMLFile<TLink>(this IEnumerable<TLink> sourceFiles,IModelSpeech modelSpeech) where TLink:IAudioFileLink 
	        => modelSpeech.Defer(() => sourceFiles.WhereNotDefault(link => link?.File).Where(link => File.Exists(link.File.FullName))
		        .ToNowObservable().BufferUntilCompleted(true).WaitUntilInactive(TimeSpan.FromSeconds(1))
		        .Select(links => links.Select(link => (link, reader: new AudioFileReader(link.File.FullName))).ToArray())
		        .SelectMany(readers => Observable.Using(
			        () => new CompositeDisposable(readers.Select(t => t.reader)), _ => {
				        var firstSpeechText = readers.First().link.To<SpeechText>();
				        var filename = $"{modelSpeech.DefaultStorageFolder}\\{firstSpeechText.File.Oid}_{readers.Last().link.File.Oid}.wav";
				        WaveFileWriter.CreateWaveFile16(filename, new ConcatenatingSampleProvider(readers.Select(t => t.reader)));
				        
				        return readers.Select(t => t.link).Cast<SpeechText>().ToArray().NewSSMLFile(  filename);
			        })))
		        .RetryWithBackoff(3);

        private static IObservable<SSMLFile> NewSSMLFile(this  SpeechText[] readers, string fileName)  
	        => Observable.Using(() => new AudioFileReader(fileName), reader => 
		        readers.NewSSMLFile(readers.First().SpeechToText, fileName,reader.TotalTime).ReturnObservable());

        private static IObservable<SpeechSynthesisResult> NotifyWhenSynthesisCanceled(this SpeechSynthesizer speechSynthesizer) 
	        => speechSynthesizer.WhenSynthesisCanceled().Select(args => args.Result)
		        .Select(result => new SpeechException($"{result.Reason}, {result.AudioData.Length}, {result.AudioDuration}"))
		        .SelectMany(Observable.Throw<SpeechSynthesisResult>);

        private static SSMLFile NewSSMLFile<TFileLink>(this IEnumerable<TFileLink> speechTexts,SpeechToText speechToText, string path, TimeSpan duration) where TFileLink:IAudioFileLink{
	        var audioFileLinks = speechTexts as TFileLink[] ?? speechTexts.ToArray();
	        var speechLanguage = audioFileLinks.First().Language;
	        var ssmlFile = speechLanguage.ObjectSpace.EnsureObject<SSMLFile>(file => file.File.FileName == path);
	        ssmlFile.Duration = duration;
	        ssmlFile.Language = ssmlFile.ObjectSpace.GetObject(speechLanguage);
	        ssmlFile.File = ssmlFile.CreateObject<FileLinkObject>();
	        ssmlFile.File.FileName = Path.GetFileName(path);
	        ssmlFile.File.FullName = path;
	        ssmlFile.SpeechTexts.AddRange(ssmlFile.ObjectSpace.GetObject(speechToText).Texts.Cast<TFileLink>()
		        .Where(audioFileLinks.Contains).DistinctBy(text => text.Oid).Cast<SpeechText>());
	        ssmlFile.CommitChanges();
	        return ssmlFile;
        }

        [SuppressMessage("ReSharper", "HeapView.CanAvoidClosure")]
        private static IObservable<Unit> SpeakSSML(this SingleChoiceAction sayItAction, Func<SpeechText[]> speechTextSourceSelector) 
	        => sayItAction.StopSpeak("SSML").MergeToUnit(sayItAction.WhenExecuted().Where(e => (string)e.SelectedChoiceActionItem.Data=="SSML")
		        .Where(e => e.Action.CommonImage()==CommonImage.Start).Do(e => e.Action.SetImage(CommonImage.Stop))
		        .SelectMany(e => e.Action.SpeakSSML(speechTextSourceSelector)
			        .DoOnComplete(() => e.Action.SetImage(CommonImage.Start)))
		        .ToUnit());

        private static IObservable<Unit> SpeakText(this SingleChoiceAction sayItAction,Func<SpeechText[]> speechTextSelector) 
	        => sayItAction.StopSpeak("Text",synthesizer => synthesizer.Properties.SetProperty("Stop","true"),synthesizer => synthesizer.Properties.GetProperty("Start")=="true")
		        .MergeToUnit(sayItAction.WhenExecuted().Where(e => (string)e.SelectedChoiceActionItem.Data=="Text")
			        .Where(e => e.Action.CommonImage()==CommonImage.Start).Do(e => e.Action.SetImage(CommonImage.Stop))
			        .SelectMany(e => e.ResetSynthesizerProperties()).Select(e => (e,context:SynchronizationContext.Current))
			        .SelectMany(t => sayItAction.SpeakText( speechTextSelector, t.context)
				        .BufferUntilCompleted().ObserveOnContext()
				        .Do(_ => t.e.Action.SetImage(CommonImage.Start))
				        .SelectMany().Do(synthesizer => {
					        synthesizer.Properties.SetProperty("Start", "false");
					        synthesizer.Properties.SetProperty("Stop", "false");
				        })));

        private static IObservable<SpeechSynthesizer> SpeakText(this SingleChoiceAction sayItAction, Func<SpeechText[]> speechTextSelector, SynchronizationContext context) 
	        => speechTextSelector().ToNowObservable()
		        .SelectManySequential(speechText => {
			        var speechVoice = speechText.SpeechVoice();
			        var speechSynthesizer = speechVoice.SpeechSynthesizer(sayItAction.View().ObjectTypeInfo.Type);
			        speechSynthesizer.Properties.SetProperty("Start", "true");
			        return Observable.If(() => speechSynthesizer.Properties.GetProperty("Stop") == "false",
				        speechSynthesizer.Defer(() => speechSynthesizer.SpeakTextAsync(speechText.Text)
					        .ToObservable().ObserveOn(context!)
					        .SelectMany(result => sayItAction.SaveTextAudioFile(speechVoice, speechText, result))
					        .TakeUntil(speechSynthesizer.WhenSynthesisCanceled()).To(speechSynthesizer)));
		        });

        private static IObservable<Unit> SpeechToTextAction(this ApplicationModulesManager manager,string actionId,CommonImage commonImage,Func<AudioConfig, SpeechToText, SynchronizationContext, SimpleAction, IObservable<Unit>> operation) 
            => manager.RegisterViewSimpleAction(actionId,action => {
                    action.TargetObjectType = typeof(SpeechToText);
                    action.TargetViewType=ViewType.DetailView;
                    action.SetImage(commonImage);
                    action.SetTargetCriteria<SpeechToText>(speechToText => !speechToText.IsNewObject&&speechToText.IsValid);
                },PredefinedCategory.ObjectsCreation)
                .MergeToUnit(manager.WhenSpeechToTextAction(actionId,commonImage, operation ));
        
        private static IObservable<Unit> WhenSpeechToTextAction(this ApplicationModulesManager manager, string actionId,
	        CommonImage startImage, Func<AudioConfig, SpeechToText, SynchronizationContext, SimpleAction, IObservable<Unit>> operation) 
	        => manager.WhenSpeechApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechToText),ViewType.DetailView)
		        .SelectUntilViewClosed(frame => frame.SimpleAction(actionId)
			        .WhenExecuted().Where(e => e.Action.CommonImage()==startImage).Do(e => e.Action.SetImage(CommonImage.Stop))
			        .Select(e => (speechToText:e.View().CurrentObject.To<SpeechToText>(),action:e.Action.ToSimpleAction(),context:SynchronizationContext.Current))
			        .SelectMany(t => Observable.Using(() => t.speechToText.AudioConfig(),audioConfig => operation(audioConfig, t.speechToText, t.context,t.action)))));

        private static IObservable<Unit> SaveTextAudioFile(this SingleChoiceAction sayItAction, SpeechVoice speechVoice, SpeechText speechText, SpeechSynthesisResult result) {
	        var path = $"{sayItAction.Application.Model.SpeechModel().DefaultStorageFolder}\\{speechVoice.Oid}{speechText.Oid}.wav";
	        if (File.Exists(path)) {
		        File.Delete(path);
	        }
	        return File.WriteAllBytesAsync(path, result.AudioData).ToObservable()
		        .ObserveOnContext()
		        .Do(_ => {
			        speechText.File ??= speechText.ObjectSpace.CreateObject<FileLinkObject>();
			        speechText.File.FullName = path;
			        speechText.File.FileName = Path.GetFileName(path);
			        speechText.VoiceDuration = speechText.File.Duration();
			        speechText.CommitChanges();
		        });
        }
	}
}