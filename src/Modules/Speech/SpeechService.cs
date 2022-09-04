using System;
using System.Collections.Concurrent;
using Swordfish.NET.Collections.Auxiliary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using HarmonyLib;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using Xpand.Extensions.DateTimeExtensions;
using Xpand.Extensions.FileExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.Extensions.XAF.Xpo.Xpo;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Speech.BusinessObjects;
using ListView = DevExpress.ExpressApp.ListView;
using View = DevExpress.ExpressApp.View;

namespace Xpand.XAF.Modules.Speech{
    internal static class SpeechService{
	    public static SimpleAction SpeechToText(this (SpeechModule, Frame frame) tuple) 
		    => tuple.frame.Action(nameof(SpeechToText)).To<SimpleAction>();
	    public static SimpleAction Translate(this (SpeechModule, Frame frame) tuple) 
		    => tuple.frame.Action(nameof(Translate)).To<SimpleAction>();
	    public static SimpleAction SelectInExplorer(this (SpeechModule, Frame frame) tuple) 
		    => tuple.frame.Action(nameof(SelectInExplorer)).To<SimpleAction>();
	    public static SingleChoiceAction SayIt(this (SpeechModule, Frame frame) tuple) 
		    => tuple.frame.Action(nameof(SayIt)).To<SingleChoiceAction>();
	    
	    public static SimpleAction Speak(this (SpeechModule, Frame frame) tuple) 
		    => tuple.frame.Action(nameof(SpeakText)).To<SimpleAction>();
        internal static IObservable<Unit> Connect(this  ApplicationModulesManager manager)
	        => manager.SpeechToTextAction(nameof(SpeechToText),CommonImage.ConvertTo,Recognize() )
		        .Merge(manager.SpeechToTextAction(nameof(Translate),CommonImage.Language,Translate() ))
		        .Merge(manager.SayIt())
		        .Merge(manager.SpeechSynthesizerCache())
		        .Merge(manager.SSML())
		        .Merge(manager.ConfigureSpeechTextView())
			    .Merge(manager.Speak())
		        .Merge(manager.UpdateAccounts())
			    .Merge(manager.SpeechTextInfo())
			    .Merge(manager.SelectInExplorer())
        ;

        private static Func<AudioConfig, SpeechToText, SynchronizationContext, SimpleAction, IObservable<Unit>> Recognize() 
	        => (audioConfig, speechToText, context, action) =>speechToText.Recognize( audioConfig, context, action);
        private static Func<AudioConfig, SpeechToText, SynchronizationContext, SimpleAction, IObservable<Unit>> Translate() 
	        => (audioConfig, speechToText, context, action) =>speechToText.Translate( audioConfig, context, action);

        private static IObservable<Unit> SpeechTextInfo(this ApplicationModulesManager manager)
	        => manager.WhenApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechToText),ViewType.DetailView)
			        .SelectUntilViewClosed(frame => frame.View.ToDetailView().NestedFrameContainers(typeof(SpeechText))
			        .SelectMany(container => container.Frame.View.WhenSelectionChanged(1)
				        .Select(view => view.SelectedObjects.Cast<SpeechText>().ToArray())
				        .StartWith(container.Frame.View.SelectedObjects.Cast<SpeechText>().ToArray()).WhenNotEmpty()
				        .Do(speechTexts => frame.View.NewSpeechInfo(container.Frame.View.ObjectTypeInfo.Type,speechTexts)))))
		        .ToUnit();

        private static void NewSpeechInfo(this View view,Type speechTextType, params SpeechText[] speechTexts) {
	        var speechToText = view.CurrentObject.To<SpeechToText>();
	        var speechTextInfos = speechToText.SpeechInfo;
	        string type=speechTextType==typeof(SpeechTranslation)?"Translation":"Recognition";
	        speechTextInfos.Remove(speechTextInfos.FirstOrDefault(info => info.SpeechType == type));
	        var speechTextInfo = speechToText.ObjectSpace.CreateObject<SpeechTextInfo>();
	        speechTextInfo.SpeechType = type;
	        speechTextInfo.SelectedLines = speechTexts.Length;
	        speechTextInfo.TotalLines = view.SpeechTextFrame( speechTextType).View?.ToListView().CollectionSource.Objects().Count()??0;
	        speechTextInfo.Duration = speechTexts.Sum(text => text.Duration.Ticks).TimeSpan();
	        speechTextInfo.SSMLDuration = speechTexts.Sum(text => text.Offset + text.Duration.Ticks).TimeSpan();
	        speechTextInfos.Add(speechTextInfo);
	        speechToText.ObjectSpace.CommitChanges();
        }

        private static Frame SpeechTextFrame(this View view, Type speechTextType) 
	        => view.AsDetailView().GetItems<IFrameContainer>().WhereNotDefault(container => container.Frame)
		        .Where(container => container.Frame.View.ObjectTypeInfo.Type == speechTextType)
		        .Select(container => container.Frame).FirstOrDefault();

        private static IObservable<Unit> SelectInExplorer(this ApplicationModulesManager manager)
	        => manager.RegisterViewSimpleAction(nameof(SelectInExplorer), action => {
			        action.TargetObjectType = typeof(ISelectInExplorer);
			        action.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
			        action.SetTargetCriteria<ISelectInExplorer>(text => text.File != null);
			        action.SetImage(CommonImage.Find);
		        })
		        .WhenConcatExecution(e => e.SelectedObjects.Cast<ISelectInExplorer>().WhereNotDefault(text => text.File).ToNowObservable()
			        .Do(text => new FileInfo(text.File.FullName).SelectInExplorer()))
		        .ToUnit();
        private static IObservable<Unit> ConfigureSpeechTextView(this ApplicationModulesManager manager)
			=> manager.WhenApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechToText), ViewType.DetailView)
					.SelectUntilViewClosed(frame => frame.View.ToDetailView().NestedFrameContainers(typeof(SpeechText))
						.Do(container => ((XPObjectSpace)container.Frame.View.ObjectSpace).PopulateAdditionalObjectSpaces(application)))
					.MergeToUnit(application.SynchronizeSpeechTextListViewSelection()))
				.MergeToUnit(manager.ConfigureSpeechTranslationView());
        
        private static IObservable<Unit> ConfigureSpeechTranslationView(this ApplicationModulesManager manager)
	        => manager.WhenApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechToText), ViewType.DetailView)
			        .SelectUntilViewClosed(frame => frame.View.ToDetailView().NestedFrameContainers(typeof(SpeechTranslation))
				        .Do(container => container.Frame.View.ToListView().CollectionSource
					        .SetCriteria<SpeechTranslation>(translation => translation.SpeechToText.Oid== ((SpeechToText)frame.View.CurrentObject).Oid)))
		        ).ToUnit()
		        .MergeToUnit(manager.WhenApplication(application => application.WhenDetailViewCreating(typeof(SpeechToText))
			        .SelectUntilViewClosed(t => application.WhenListViewCreating(typeof(SpeechTranslation)).Where(t1 => t1.e.ObjectSpace is not INestedObjectSpace)
				        .Do(t1 => {
					        var nestedObjectSpace = t.e.ObjectSpace.CreateNestedObjectSpace();
					        t1.e.ObjectSpace.Dispose();
					        t1.e.View = (ListView)application.NewView(application.Model.Views[t1.e.ViewID], nestedObjectSpace);
				        }))
			        ))
		        .MergeToUnit(manager.WhenApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechToText),ViewType.DetailView)
			        .SelectUntilViewClosed(frame => frame.View.ObjectSpace.WhenCommitted<SpeechText>(ObjectModification.Deleted)
				        .Do(_ => frame.GetController<RefreshController>().RefreshAction.DoExecute()))));
        private static IObservable<Unit> UpdateAccounts(this ApplicationModulesManager manager) 
	        => manager.WhenApplication(application => application.WhenCommitted<SpeechAccount>(ObjectModification.New)
			        .ToObjects().Select(account => (account,SynchronizationContext.Current))
			        .SelectMany(t => Observable.Using(() => new SpeechSynthesizer(t.account.SpeechConfig()),synthesizer => synthesizer.GetVoicesAsync().ToObservable()
				        .ObserveOnContext(t.Current)
				        .SelectMany(result => result.Voices.ToNowObservable().Do(info =>t.account.NewVoice(info) ).BufferUntilCompleted().Do(_ => t.account.CommitChanges())))))
		        .ToUnit();

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

        private static SpeechAccount DefaultAccount(this IObjectSpace space,XafApplication application) 
	        => space.FindObject<SpeechAccount>(CriteriaOperator.Parse(application.Model.SpeechModel().DefaultAccountCriteria));

        private static IObservable<Unit> Speak(this SpeechAccount defaultAccount, IModelSpeech speechModel) 
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

        private static IObservable<Unit> SSML(this ApplicationModulesManager manager) 
	        => manager.WhenApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechText),ViewType.ListView)
			        .SelectUntilViewClosed(frame => frame.View.WhenSelectionChanged(1)
				        .Do(view => view.AsListView().CollectionSource.Objects<SpeechText>().FirstOrDefault()?.SpeechToText.TranslationSSMLs.Clear())
				        .SelectMany(view => view.SelectedObjects.Cast<SpeechText>().GroupBy(speechText => speechText.Language()).ToNowObservable()
					        .WhenNotDefault(speechTexts => speechTexts.Key).Do(speechTexts => speechTexts.UpdateSSML(application.SSMLText)
						        .ObjectSpace.SetIsModified(false)))))
		        .ToUnit();

        private static SpeechText UpdateSSML(this IGrouping<SpeechLanguage, SpeechText> speechTexts, Func<SpeechText,SpeechText[],string> ssmlText) {
	        var speechText = speechTexts.First();
	        var array = speechTexts.ToArray();
	        var text = ssmlText(speechText,array);
	        var ssml = speechText.ObjectSpace.CreateObject<SSML>();
	        ssml.Language = speechTexts.Key;
	        ssml.SpeechTexts.AddRange(array.Select(text1 => text1.Oid));
	        ssml.Text = text;
	        if (speechText.GetType() == typeof(SpeechTranslation)) {
		        speechText.SpeechToText.TranslationSSMLs.Add(ssml);
	        }
	        else {
		        speechText.SpeechToText.SSML = ssml;
	        }

	        return speechText;
        }

        private static string SSMLText(this XafApplication application, SpeechText speechText, IEnumerable<SpeechText> speechTexts) {
	        var speechModel = application.Model.SpeechModel();
	        var voiceText = speechTexts.ToNowObservable().CombineWithPrevious().WhenNotDefault(t => t.previous)
		        .Select(t => $"<break time=\"{t.current.Start.Subtract(t.previous.End).TotalSeconds}s\" />{t.current.Text}")
		        .ToEnumerable().ToArray().Join();
	        return speechModel.SSMLSpeakFormat.StringFormat(
		        speechModel.SSMLVoiceFormat.StringFormat(speechText.SpeechVoice()?.ShortName, $"{speechText.Text}{voiceText}"));
        }

        private static SpeechVoice SpeechVoice(this SpeechText speechText) {
	        var speechLanguage = speechText.Language();
	        var voices =speechText is SpeechTranslation?speechText.SpeechToText.SpeechVoices: speechText.SpeechToText.Account.Voices;
	        return voices.FirstOrDefault(voice => voice.Language.Name == speechLanguage.Name);
        }
        private static SpeechVoice SpeechVoice(this SpeechToText speechToText,SpeechLanguage speechLanguage) 
	        => speechToText.SpeechVoices.FirstOrDefault(voice => voice.Language.Name == speechLanguage.Name)??speechToText.Account.Voices.First(voice => voice.Language.Name==speechLanguage.Name);

        private static SpeechLanguage Language(this SpeechText speechText)
			=> speechText is SpeechTranslation translation?translation.Language:speechText.SpeechToText.RecognitionLanguage;

        private static readonly ConcurrentDictionary<(string voice,Type speechTextType),SpeechSynthesizer> SpeechSynthesizersCache = new();
        [SuppressMessage("ReSharper", "HeapView.CanAvoidClosure")]
        private static IObservable<Unit> SpeechSynthesizerCache(this ApplicationModulesManager manager)
			=> manager.WhenApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechToText),ViewType.DetailView)
				.Select(frame => frame.View.CurrentObject.To<SpeechToText>())
				.SelectMany(speechToText => speechToText.WhenVoices()
					.SelectMany(voice => new[]{typeof(SpeechText),typeof(SpeechTranslation)}.ToObservable()
						.Do(type => SpeechSynthesizersCache.GetOrAdd((voice.ShortName,type), _ => {
							var speechSynthesizer = new SpeechSynthesizer(voice.SpeechConfig(speechToText.Account));
							speechSynthesizer.Properties.SetProperty("Start","false");
							speechSynthesizer.Properties.SetProperty("Stop","false");
							return speechSynthesizer;
						})))).ToUnit());

        private static IObservable<SpeechVoice> WhenVoices(this SpeechToText speechToText) {
	        var modifiedSpeechToText = speechToText.ObjectSpace.WhenModifiedObjects<SpeechToText>().WhenNotDefault(text => text.Account)
		        .SelectMany(text => text.SpeechVoices.AddItem(text.Account.Voices.First(voice => voice.Language.Oid==text.RecognitionLanguage?.Oid)));
	        var existingVoices = speechToText.Account == null ? Observable.Empty<SpeechVoice>()
		        : speechToText.SpeechVoices.AddItem(speechToText.Account.Voices.First(voice => voice.Language.Oid == speechToText.RecognitionLanguage?.Oid)).ToNowObservable();
	        return existingVoices.Concat(modifiedSpeechToText).Distinct(voice => voice.ShortName);
        }

        private static SpeechConfig SpeechConfig(this SpeechVoice voice,SpeechAccount account) {
	        var speechConfig = account.SpeechConfig();
	        speechConfig.SpeechSynthesisVoiceName = voice.ShortName;
	        speechConfig.SpeechSynthesisLanguage = voice.Language.Name;
	        return speechConfig;
        }

        private static IObservable<Unit> SayIt(this ApplicationModulesManager manager) 
	        => manager.RegisterViewSingleChoiceAction(nameof(SayIt), action => {
			        action.TargetObjectType = typeof(SpeechText);
			        action.TargetViewType=ViewType.ListView;
			        action.SelectionDependencyType=SelectionDependencyType.RequireMultipleObjects;
			        action.ItemType=SingleChoiceActionItemType.ItemIsOperation;
			        action.PaintStyle=ActionItemPaintStyle.Image;
			        action.DefaultItemMode=DefaultItemMode.LastExecutedItem;
			        action.SetImage(CommonImage.Start);
			        action.Items.Add(new ChoiceActionItem("Text","Text"));
			        action.Items.Add(new ChoiceActionItem("SSML","SSML"));
		        },PredefinedCategory.ObjectsCreation)
		        .ToUnit()
		        .Merge(manager.WhenApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechToText),ViewType.DetailView)
			        .SelectUntilViewClosed(speechToTextFrame => speechToTextFrame.View.AsDetailView().NestedFrameContainers(typeof(SpeechText))
				        .SelectMany(editor => editor.Frame.SingleChoiceAction(nameof(SayIt)).SayIt( speechToTextFrame.View.CurrentObject.To<SpeechToText>()))
				        .ToUnit())));

        private static IObservable<SpeechSynthesisResult> SayIt(this SpeechText speechText) 
	        => Observable.Using(() => new SpeechSynthesizer(speechText.SpeechConfig()),synthesizer 
		        => synthesizer.SpeakTextAsync(speechText.Text).ToObservable().Select(result => result));

        private static SpeechConfig SpeechConfig(this SpeechText speechText) {
	        var speechConfig = speechText.SpeechToText.Account.SpeechConfig();
	        speechConfig.SpeechSynthesisLanguage = speechText is SpeechTranslation translation
		        ? translation.Language.Name : speechText.SpeechToText.RecognitionLanguage.Name;
	        speechConfig.SpeechSynthesisVoiceName = $"{speechText.SpeechVoice()?.ShortName}";
	        return speechConfig;
        }

        private static IObservable<Unit> SynchronizeSpeechTextListViewSelection(this XafApplication application) 
	        => application.WhenNestedListViewsSelectionChanged<SpeechText, SpeechTranslation>((speechText, speechTranslation) => speechText.Start == speechTranslation.SourceText?.Start)
		        .SynchronizeGridListEditor().ToUnit();
        
        private static IObservable<Unit> SpeechToTextAction(this ApplicationModulesManager manager,string actionId,CommonImage commonImage,Func<AudioConfig, SpeechToText, SynchronizationContext, SimpleAction, IObservable<Unit>> operation) 
	        => manager.RegisterViewSimpleAction(actionId,action => {
			        action.TargetObjectType = typeof(SpeechToText);
			        action.TargetViewType=ViewType.DetailView;
			        action.SetImage(commonImage);
			        action.SetTargetCriteria<SpeechToText>(text => !text.IsNewObject&&text.File!=null);
			        action.Category = action.TargetObjectType.Name;
		        })
		        .MergeToUnit(manager.WhenSpeechToTextAction(actionId,commonImage, operation ));

        private static IObservable<Unit> WhenSpeechToTextAction(this ApplicationModulesManager manager, string actionId,
	        CommonImage startImage, Func<AudioConfig, SpeechToText, SynchronizationContext, SimpleAction, IObservable<Unit>> operation) 
	        => manager.WhenApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechToText),ViewType.DetailView)
		        .SelectUntilViewClosed(frame => frame.SimpleAction(actionId)
			        .WhenExecuted().Where(e => e.Action.CommonImage()==startImage).Do(e => e.Action.SetImage(CommonImage.Stop))
			        .Select(e => (speechToText:e.View().CurrentObject.To<SpeechToText>(),action:e.Action.ToSimpleAction(),context:SynchronizationContext.Current))
			        .SelectMany(t => Observable.Using(() => t.speechToText.AudioConfig(),audioConfig => operation(audioConfig, t.speechToText, t.context,t.action)))));

        private static IObservable<Unit> Translate(this SpeechToText speechToText,AudioConfig audioConfig, SynchronizationContext context,SimpleAction simpleAction)
	        =>Observable.Using(() => new TranslationRecognizer(speechToText.TranslationConfig(),audioConfig),recognizer => recognizer.StartContinuousRecognitionAsync().ToObservable()
		        .SelectMany(_ => recognizer.WhenSessionStopped().TakeUntil(simpleAction.WhenExecuted().Where(e => e.Action.CommonImage()==CommonImage.Stop).Take(1)
				        .SelectMany(_ => recognizer.StopContinuousRecognitionAsync().ToObservable()))
			        .FirstAsync().ObserveOn(context).Do(_ => simpleAction.SetImage(CommonImage.Language))
			        .MergeToUnit(recognizer.WhenRecognized().ObserveOn(context)
				        .SelectMany((e, i) => e.Result.Translations.ToNowObservable().TraceSpeech()
					        .Select( pair => {
						        var speechTextFrame = simpleAction.View().ToDetailView().SpeechTextFrame(typeof(SpeechTranslation));
						        var newSpeechTranslation = speechTextFrame.View.ToListView().ObjectSpace.GetObject(speechToText)
							        .NewSpeechTranslation(pair, e.Result, i, e.SessionId);
						        speechTextFrame.View.ToListView().CollectionSource.Add(speechTextFrame.View.ObjectSpace.GetObject(newSpeechTranslation));
						        speechToText.CommitChanges();
						        return newSpeechTranslation;
					        })
				        ).ToUnit()
				        .IgnoreElements())));

        private static IObservable<Unit> Recognize(this SpeechToText speechToText, AudioConfig audioConfig, SynchronizationContext context, SimpleAction simpleAction)
			=> Observable.Using(() => new SpeechRecognizer(speechToText.Account.SpeechConfig(),audioConfig),recognizer => recognizer.StartContinuousRecognitionAsync().ToObservable()
				.SelectMany(_ => recognizer.WhenSessionStopped().TakeUntil(simpleAction.WhenExecuted().Where(e => e.Action.CommonImage()==CommonImage.Stop).Take(1)
						.SelectMany(_ => recognizer.StopContinuousRecognitionAsync().ToObservable()))
					.FirstAsync().ObserveOn(context).Do(_ => simpleAction.SetImage(CommonImage.ConvertTo))
					.MergeToUnit(recognizer.WhenRecognized().ObserveOn(context)
						.Do(result => speechToText.NewSpeechText<SpeechText>(result.Text, result.Duration, result.OffsetInTicks))
						.IgnoreElements())));
        
        private static IObservable<SpeechRecognitionResult> WhenRecognized(this SpeechRecognizer recognizer) 
	        => recognizer.WhenEvent<SpeechRecognitionEventArgs>(nameof(SpeechRecognizer.Recognized)).Select(args => args.Result);

        private static IObservable<Unit> WhenSessionStopped(this Recognizer recognizer) 
	        => recognizer.WhenEvent(nameof(Recognizer.SessionStopped)).ToUnit().TraceSpeech();

        private static IObservable<Unit> SayIt(this SingleChoiceAction sayItAction, SpeechToText speechToText)  
	        => sayItAction.SpeakText().Merge(sayItAction.SpeakSSML(speechToText));
        
        [SuppressMessage("ReSharper", "HeapView.CanAvoidClosure")]
        private static IObservable<Unit> SpeakSSML(this SingleChoiceAction sayItAction, SpeechToText speechToText) 
	        => sayItAction.StopSpeak("SSML").MergeToUnit(sayItAction.WhenExecuted().Where(e => (string)e.SelectedChoiceActionItem.Data=="SSML")
		        .Where(e => e.Action.CommonImage()==CommonImage.Start).Do(e => e.Action.SetImage(CommonImage.Stop))
		        .SelectMany(e => e.Action.View().SelectedObjects.Cast<SpeechText>().SSMLs()
			        .ToNowObservable()
			        .SelectMany(ssml => {
				        var speechVoice = speechToText.SpeechVoice(ssml.Language);
				        var speechSynthesizer = speechVoice.SpeechSynthesizer(e.Action.View().ObjectTypeInfo.Type);
				        var path = $"{sayItAction.Application.Model.SpeechModel().DefaultStorageFolder}\\{speechVoice.Oid}_{ssml.SpeechTexts.First()}_{ssml.SpeechTexts.Last()}.wav";
				        return speechSynthesizer.SpeakSsmlAsync(ssml.Text).ToObservable()
					        .BufferUntilCompleted().SelectMany()
					        .DoWhen(_ => File.Exists(path), _ => File.Delete(path))
					        .SelectMany(result => File.WriteAllBytesAsync(path, result.AudioData).ToObservable().To(result).FirstAsync())
					        .ObserveOnContext().Select(result => {
						        var ssmlFile = ssml.NewSSMLFile(speechToText, speechVoice, path, result.AudioDuration);
						        ssmlFile.SpeechTexts.First().SpeechToText.FireChanged(nameof(BusinessObjects.SpeechToText.SSMLFiles));
						        return ssmlFile;
					        })
					        .Do(_ => e.Action.SetImage(CommonImage.Start));
			        }))
		        .ToUnit());

        private static IObservable<Unit> StopSpeak(this SingleChoiceAction sayItAction,string data,Action<SpeechSynthesizer> configure=null,Func<SpeechSynthesizer,bool> filter=null) 
	        => sayItAction.WhenExecuted().Where(e => e.Action.CommonImage()==CommonImage.Stop&&(string)e.SelectedChoiceActionItem.Data==data)
		        .SelectMany(e => e.Action.View().ToListView().CollectionSource.Objects<SpeechText>().Select(text => text.SpeechVoice()).DistinctBy(voice => voice.Name).ToNowObservable()
			        .Select(voice => voice.SpeechSynthesizer(e.Action.View().ObjectTypeInfo.Type)).Do(synthesizer => configure?.Invoke(synthesizer)))
		        .Where(synthesizer => filter?.Invoke(synthesizer)??true)
		        .SelectMany(synthesizer => synthesizer.StopSpeakingAsync().ToObservable()).IgnoreElements();

        private static IObservable<Unit> SpeakText(this SingleChoiceAction sayItAction) 
	        => sayItAction.StopSpeak("Text",synthesizer => synthesizer.Properties.SetProperty("Stop","true"),synthesizer => synthesizer.Properties.GetProperty("Start")=="true")
		        .MergeToUnit(sayItAction.WhenExecuted().Where(e => (string)e.SelectedChoiceActionItem.Data=="Text")
		        .Where(e => e.Action.CommonImage()==CommonImage.Start).Do(e => e.Action.SetImage(CommonImage.Stop))
		        .SelectMany(e => e.ResetSynthesizerProperties()).Select(e => (e,context:SynchronizationContext.Current))
		        .SelectMany(t => t.e.SelectedObjects.Cast<SpeechText>().ToNowObservable()
			        .SelectManySequential(speechText => {
				        var speechVoice = speechText.SpeechVoice();
				        var speechSynthesizer = speechVoice.SpeechSynthesizer(t.e.Action.View().ObjectTypeInfo.Type);
				        speechSynthesizer.Properties.SetProperty("Start","true");
				        return Observable.If(() => speechSynthesizer.Properties.GetProperty("Stop")=="false",
					        speechSynthesizer.Defer(() => speechSynthesizer.SpeakTextAsync(speechText.Text).ToObservable().ObserveOn(t.context!)
						        .SelectMany(result => sayItAction.SaveAudioFile( speechVoice, speechText, result))
						        .TakeUntil(speechSynthesizer.WhenSynthesisCanceled()).To(speechSynthesizer)));
			        })
			        .BufferUntilCompleted().ObserveOnContext()
			        .Do(_ => t.e.Action.SetImage(CommonImage.Start))
			        .SelectMany().Do(synthesizer => {
				        synthesizer.Properties.SetProperty("Start", "false");
				        synthesizer.Properties.SetProperty("Stop", "false");
			        })
		        ));

        private static IObservable<Unit> SaveAudioFile(this SingleChoiceAction sayItAction, SpeechVoice speechVoice, SpeechText speechText, SpeechSynthesisResult result) {
	        var path = $"{sayItAction.Application.Model.SpeechModel().DefaultStorageFolder}\\{speechVoice.Oid}{speechText.Oid}.wav";
	        if (File.Exists(path)) {
		        File.Delete(path);
	        }
	        speechText.AudioDuration = result.AudioDuration;
	        speechText.File = speechText.ObjectSpace.CreateObject<FileLinkObject>();
	        speechText.File.FullName = path;
	        speechText.File.FileName = Path.GetFileName(path);
	        return speechText.Commit().Merge(File.WriteAllBytesAsync(path, result.AudioData).ToObservable());
        }

        private static IEnumerable<SSML> SSMLs(this IEnumerable<SpeechText> speechTexts) {
	        var speechText = speechTexts.First();
	        return speechText is SpeechTranslation ? speechText.SpeechToText.TranslationSSMLs : speechText.SpeechToText.SSML.YieldItem();
        }

        private static SSMLFile NewSSMLFile(this SSML ssml,SpeechToText speechToText, SpeechVoice speechVoice, string path, TimeSpan audioDuration) {
	        var ssmlFile = speechVoice.ObjectSpace.EnsureObject<SSMLFile>(file => file.File.FileName == path);
	        ssmlFile.Duration = audioDuration;
	        ssmlFile.Language = ssmlFile.ObjectSpace.GetObject(speechVoice.Language);
	        ssmlFile.File = ssmlFile.CreateObject<FileLinkObject>();
	        ssmlFile.File.FileName = Path.GetFileName(path);
	        ssmlFile.File.FullName = path;
	        ssmlFile.SpeechTexts.AddRange(ssmlFile.ObjectSpace.GetObject(speechToText).Texts
		        .Where(text => ssml.SpeechTexts.Contains(text.Oid)));
	        ssmlFile.CommitChanges();
	        return ssmlFile;
        }

        public static SpeechSynthesizer SpeechSynthesizer(this SpeechVoice speechVoice,Type speechTextType) {
	        SpeechSynthesizersCache.TryGetValue((speechVoice.ShortName, speechTextType), out var value);
	        return value;
        }
        
        private static IObservable<SingleChoiceActionExecuteEventArgs> ResetSynthesizerProperties(this SingleChoiceActionExecuteEventArgs e) 
	        => e.Action.View().ToListView().CollectionSource.Objects<SpeechText>()
		        .Select(text => text.SpeechVoice()).DistinctBy(voice => voice.Name).ToNowObservable()
		        .Select(voice => voice.SpeechSynthesizer(e.Action.View().ObjectTypeInfo.Type))
		        .Do(synthesizer => {
			        synthesizer.Properties.SetProperty("Start", "false");
			        synthesizer.Properties.SetProperty("Stop", "false");
		        })
		        .ConcatIgnoredValue(e);
        
        private static IObservable<SpeechSynthesisEventArgs> WhenSynthesisCanceled(this SpeechSynthesizer synthesizer) 
	        => synthesizer.WhenEvent<SpeechSynthesisEventArgs>(nameof(Microsoft.CognitiveServices.Speech.SpeechSynthesizer.SynthesisCanceled)).TraceSpeech();

        public static IModelSpeech SpeechModel(this IModelApplication applicationModel) 
	        => applicationModel.ToReactiveModule<IModelReactiveModuleSpeech>().Speech;

        public static SpeechConfig SpeechConfig(this SpeechAccount account) 
	        => Microsoft.CognitiveServices.Speech.SpeechConfig.FromSubscription(account.Subscription, account.Region);
        
        private static IObservable<TranslationRecognitionEventArgs> WhenRecognized(this TranslationRecognizer translationRecognizer) 
	        => translationRecognizer.WhenEvent<TranslationRecognitionEventArgs>(nameof(TranslationRecognizer.Recognized));

        private static SpeechTranslation NewSpeechTranslation(this SpeechToText speechToText, KeyValuePair<string, string> pair,
			TranslationRecognitionResult result, int i, string sessionId) {
			var speechTranslation = speechToText.NewSpeechText<SpeechTranslation>(pair.Value, result.Duration, result.OffsetInTicks);
			speechTranslation.SessionId = sessionId;
			speechTranslation.Language=speechToText.TargetLanguages.FirstOrDefault(language => language.Name.Split('-')[0] == pair.Key);
			var sourceText = speechToText.SpeechTexts[i];
			sourceText.RealTranslations.Add(speechTranslation);
			sourceText.Translations.Add(speechTranslation);
			return speechTranslation;
        }
        
		private static AudioConfig AudioConfig(this SpeechToText speechToText) 
			=> Microsoft.CognitiveServices.Speech.Audio.AudioConfig.FromWavFileInput(speechToText.File.FullName);

		private static SpeechTranslationConfig TranslationConfig(this SpeechToText speechToText) {
			var translationConfig = speechToText.Account.TranslationConfig();
			translationConfig.SpeechRecognitionLanguage = speechToText.RecognitionLanguage.Name;
			speechToText.TargetLanguages.ForEach(language => translationConfig.AddTargetLanguage(language.Name));
			return translationConfig;
		}

		private static SpeechTranslationConfig TranslationConfig(this SpeechAccount account) 
			=> SpeechTranslationConfig.FromSubscription(account.Subscription, account.Region);
		
		private static T NewSpeechText<T>(this SpeechToText speechToText, string text,TimeSpan duration, long offset) where T:SpeechText{
			var speechText = speechToText.ObjectSpace.CreateObject<T>();
			speechText.SpeechToText = speechToText;
			speechToText.SpeechTexts.Add(speechText);
			speechText.Text = text;
			speechText.Offset=offset;
			speechText.Duration = duration;
			speechText.CommitChanges();
			speechToText.FireChanged(nameof(BusinessObjects.SpeechToText.SpeechTexts));
			return speechText;
		}

		internal static IObservable<TSource> TraceSpeech<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
	        => source.Trace(name, SpeechModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
    }

}