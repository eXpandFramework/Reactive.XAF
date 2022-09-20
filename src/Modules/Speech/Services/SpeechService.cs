using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.Data.Extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using HarmonyLib;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Translation;
using NAudio.Wave;
using Xpand.Extensions.FileExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Speech.BusinessObjects;

namespace Xpand.XAF.Modules.Speech.Services{
	internal static class SpeechService{
	    
	    public static SimpleAction SelectInExplorer(this (SpeechModule, Frame frame) tuple) 
		    => tuple.frame.Action(nameof(SelectInExplorer)).To<SimpleAction>();

	    
        internal static IObservable<Unit> ConnectSpeech(this  ApplicationModulesManager manager)
	        => manager.SpeechSynthesizerCache()
		        .MergeToUnit(manager.IgnoreNonPersistentMembersDataLocking(assembly => assembly==typeof(SpeechService).Assembly))
		        .Merge(manager.SelectInExplorer())
        ;

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

        public static TimeSpan Duration(this FileLinkObject fileLinkObject) {
	        using var audioFileReader = new AudioFileReader(fileLinkObject.FullName);
	        return audioFileReader.TotalTime;
        }

        private static readonly ConcurrentDictionary<(string voice,Type speechTextType),SpeechSynthesizer> SpeechSynthesizersCache = new();
        [SuppressMessage("ReSharper", "HeapView.CanAvoidClosure")]
        private static IObservable<Unit> SpeechSynthesizerCache(this ApplicationModulesManager manager)
			=> manager.WhenSpeechApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechToText),ViewType.DetailView)
				.Select(frame => frame.View.CurrentObject.To<SpeechToText>())
				.SelectMany(speechToText => speechToText.WhenVoices()
					.SelectMany(voice => new[]{typeof(SpeechText),typeof(SpeechTranslation)}.ToObservable()
						.Do(type => SpeechSynthesizersCache.GetOrAdd((voice.ShortName,type), _ => {
							var speechSynthesizer = new SpeechSynthesizer(voice.SpeechSynthesisConfig(speechToText.Account));
							speechSynthesizer.Properties.SetProperty("Start","false");
							speechSynthesizer.Properties.SetProperty("Stop","false");
							return speechSynthesizer;
						})))).ToUnit());

        private static IObservable<SpeechVoice> WhenVoices(this SpeechToText speechToText) {
	        var modifiedSpeechToText = speechToText.ObjectSpace.WhenModifiedObjects<SpeechToText>().Where(text => text.Account!=null&&text.RecognitionLanguage!=null)
		        .SelectMany(text => text.SpeechVoices.AddItem(text.Account.Voices.First(voice => voice.Language.Oid==text.RecognitionLanguage.Oid)));
	        var existingVoices = speechToText.Account == null ? Observable.Empty<SpeechVoice>()
		        : speechToText.Account.Voices.Where(voice => voice.Language.Oid == speechToText.RecognitionLanguage?.Oid).ToNowObservable()
			        .Do(voice => speechToText.SpeechVoices.AddItem(voice));
	        return existingVoices.Concat(modifiedSpeechToText).Distinct(voice => voice.ShortName);
        }

        private static SpeechConfig SpeechSynthesisConfig(this SpeechVoice voice,SpeechAccount account) {
	        var speechConfig = account.SpeechConfig(callerMember:$"{nameof(SpeechSynthesisConfig)}_{voice}");
	        speechConfig.SpeechSynthesisVoiceName = voice.ShortName;
	        speechConfig.SpeechSynthesisLanguage = voice.Language.Name;
	        return speechConfig;
        }

        public static T PreviousSpeechText<T>(this T current) where T:SpeechText{
	        var speechTexts = current.SpeechToText?.Texts.Where(text => text.GetType()==current.GetType()).Cast<T>()
		        .OrderBy(text => text.Oid).ToArray()??Array.Empty<T>();
	        var index = speechTexts.FindIndex(text => text.Oid == current.Oid);
	        return speechTexts.Where((_, i) => i<index).LastOrDefault();
        }
        public static T NextSpeechText<T>(this T current) where T:SpeechText{
	        var speechTexts = current.SpeechToText.Texts.Where(text => text.GetType()==current.GetType()).Cast<T>()
		        .OrderBy(text => text.Oid).ToArray();
	        var index = speechTexts.FindIndex(text => text.Oid == current.Oid);
	        var enumerable = speechTexts.Where((_, i) => i>index).ToArray();
	        return enumerable.FirstOrDefault();
        }
        public static TFile UpdateSSMLFile<TFile>(this TFile file, SpeechSynthesisResult result, string path) where TFile:IAudioFileLink{
	        file.File ??= file.CreateObject<FileLinkObject>();
	        file.File.FileName = Path.GetFileName(path);
	        file.File.FullName = path;
	        using var audioFileReader = new AudioFileReader(path);
	        file.FileDuration = audioFileReader.TotalTime;
	        if (file is SpeechText speechText&&speechText.SpeechToText.GetType()==typeof(SpeechToText)) {
		        file.Duration=file.FileDuration.Value;
	        }
	        file.File.SetPropertyValue(nameof(FileLinkObject.Size),(int)audioFileReader.Length) ;
	        file.FireChanged(nameof(IAudioFileLink.File));
	        return file;
        }
        public static IObservable<SpeechRecognitionResult> WhenRecognized(this SpeechRecognizer recognizer) 
	        => recognizer.WhenEvent<SpeechRecognitionEventArgs>(nameof(SpeechRecognizer.Recognized)).Select(args => args.Result);

        public static IObservable<Unit> WhenSessionStopped(this Recognizer recognizer) 
	        => recognizer.WhenEvent<SessionEventArgs>(nameof(Recognizer.SessionStopped)).TraceSpeechManager(e => e.SessionId).ToUnit();
        public static IObservable<Unit> WhenSessionStarted(this Recognizer recognizer) 
	        => recognizer.WhenEvent<SessionEventArgs>(nameof(Recognizer.SessionStarted)).TraceSpeechManager(e => e.SessionId).ToUnit();
        public static IObservable<TranslationRecognitionCanceledEventArgs> WhenCanceled(this TranslationRecognizer recognizer) 
	        => recognizer.WhenEvent<TranslationRecognitionCanceledEventArgs>(nameof(TranslationRecognizer.Canceled));

        public static IObservable<Unit> NotifyWhenCanceled(this TranslationRecognizer recognizer) 
	        => recognizer.WhenCanceled().Select(e => new SpeechException($"Id:{e.SessionId}, reason:{e.Reason}, details:{e.ErrorDetails}"))
		        .SelectMany(e => Observable.Throw<Unit>(e).TraceSpeechError().CompleteOnError());
        
        public static IObservable<SpeechSynthesisEventArgs> WhenSynthesisCompleted(this SpeechSynthesizer speechSynthesizer) 
	        => speechSynthesizer.WhenEvent<SpeechSynthesisEventArgs>(nameof(Microsoft.CognitiveServices.Speech.SpeechSynthesizer.SynthesisCompleted))
		        .TraceSpeechManager(e => e.Result.Reason.ToString());
        
        public static SpeechSynthesizer SpeechSynthesizer(this SpeechVoice speechVoice,Type speechTextType) {
	        SpeechSynthesizersCache.TryGetValue((speechVoice.ShortName, speechTextType), out var value);
	        return value;
        }

        public static IObservable<SingleChoiceActionExecuteEventArgs> ResetSynthesizerProperties(this SingleChoiceActionExecuteEventArgs e) 
	        => e.Action.View().ToListView().CollectionSource.Objects<SpeechText>()
		        .Select(text => text.SpeechVoice()).DistinctBy(voice => voice.Name).ToNowObservable()
		        .Select(voice => voice.SpeechSynthesizer(e.Action.View().ObjectTypeInfo.Type))
		        .Do(synthesizer => {
			        synthesizer.Properties.SetProperty("Start", "false");
			        synthesizer.Properties.SetProperty("Stop", "false");
		        })
		        .ConcatIgnoredValue(e);

        public static IObservable<SpeechSynthesisEventArgs> WhenSynthesisCanceled(this SpeechSynthesizer synthesizer) 
	        => synthesizer.WhenEvent<SpeechSynthesisEventArgs>(nameof(Microsoft.CognitiveServices.Speech.SpeechSynthesizer.SynthesisCanceled)).TraceSpeechManager();
        public static IObservable<SpeechSynthesisWordBoundaryEventArgs> WhenWordBoundary(this SpeechSynthesizer synthesizer) 
	        => synthesizer.WhenEvent<SpeechSynthesisWordBoundaryEventArgs>(nameof(Microsoft.CognitiveServices.Speech.SpeechSynthesizer.WordBoundary));

        public static IModelSpeech SpeechModel(this IModelApplication applicationModel) 
	        => applicationModel.ToReactiveModule<IModelReactiveModuleSpeech>().Speech;


        public static IObservable<TranslationRecognitionEventArgs> WhenRecognized(this TranslationRecognizer translationRecognizer) 
	        => translationRecognizer.WhenEvent<TranslationRecognitionEventArgs>(nameof(TranslationRecognizer.Recognized));


		internal static IObservable<TSource> TraceSpeechManager<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
	        => source.Trace(name, SpeechModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
		
		public static IObservable<T> WhenSpeechApplication<T>(this ApplicationModulesManager manager, Func<XafApplication, IObservable<T>> selector,[CallerMemberName]string memberName="")
			=> manager.WhenApplication(application => selector(application).TraceSpeechError(memberName:memberName).CompleteOnError());
        
		public static IObservable<T> SpeechCompleteOnError<T>(this IObservable<T> source,[CallerMemberName] string memberName = "")
			=> source.TraceSpeechError(memberName:memberName).CompleteOnError();

		public static IObservable<T2> SpeechPublish<T,T2>(this IObservable<T> source,Func<IObservable<T>,IObservable<T2>> selector)
			=> source.Publish(observable => selector(observable).SpeechCompleteOnError());
        
		public static IObservable<TResult> SpeechSelectMany<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
			=> source.SpeechSelectMany(arg => selector(arg).ToNowObservable());
        
		public static IObservable<TResult> SpeechSelectMany<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> selector)
			=> source.SelectMany(arg => selector(arg).SpeechCompleteOnError());
		
		public static IObservable<TSource> TraceSpeechError<TSource>(this IObservable<TSource> source,
			Func<TSource, string> messageFactory = null, string name = null, Action<ITraceEvent> traceAction = null,
			Func<Exception, string> errorMessageFactory = null, [CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
			=> source.Trace(name, SpeechModule.TraceSource,messageFactory,errorMessageFactory, traceAction, ObservableTraceStrategy.OnError, memberName,sourceFilePath,sourceLineNumber);
    }

}