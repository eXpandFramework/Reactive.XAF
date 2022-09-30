using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Attributes.Custom;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.Speech.Services;
using Xpand.XAF.Modules.SpellChecker;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {

    [CloneModelView(CloneViewType.DetailView, nameof(SpeechText)+"_MD_DetailView")]
    [CloneModelView(CloneViewType.ListView,SpeechTextBandedListView)]
    [CloneModelView(CloneViewType.ListView,SpeechTextEditorListView)]
    [CloneModelView(CloneViewType.ListView,SpeechTextBaseListView)]
    [CloneModelView(CloneViewType.DetailView,"SpeechText_Editor_DetailView")]
    [DefaultProperty(nameof(Text))][DeferredDeletion(false)][OptimisticLocking(OptimisticLockingBehavior.ConsiderOptimisticLockingField)]
    [ImageName(("Action_Export_ToText"))][CreatableItem(false)]
    [Appearance("Bold Text",AppearanceItemType.ViewItem, "1=1",TargetItems = nameof(Text),FontStyle = FontStyle.Bold,Context = SpeechTextBandedListView+","+SpeechTranslation.SpeechTranslationBandedListView)]
    [FileAttachment(nameof(File))][SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types")]
    public class SpeechText:CustomBaseObject,ISelectInExplorer ,IAudioFileLink{
        public const string SpeechTextBaseListView = "SpeechText_Base_ListView";
        public const string SpeechTextBandedListView = nameof(BusinessObjects.SpeechToText) + "_" +
                                              nameof(BusinessObjects.SpeechToText.SpeechTexts) + "_Banded_ListView";
        public const string SpeechTextEditorListView = nameof(BusinessObjects.SpeechToText) + "_" +
                                                       nameof(BusinessObjects.SpeechToText.SpeechTexts) + "_Editor_ListView";
        public SpeechText(Session session) : base(session) { }
        
        string IAudioFileLink.Storage => SpeechToText.Storage;

        [Association("SpeechText-SSMLFiles")]
        public XPCollection<SSMLFile> SSMLFiles => GetCollection<SSMLFile>();
        
        [Association("SpeechText-Translations")][Aggregated]
        public XPCollection<SpeechTranslation> RealTranslations => GetCollection<SpeechTranslation>();
        [CollectionOperationSet(AllowAdd = false)]
        public BindingList<SpeechTranslation> Translations 
            => SpeechToText.Texts.SelectMany(text => text.RealTranslations).ToBindingList();
        SpeechToText _speechToText;
        FileLinkObject _file;
        [FileTypeFilter("Audio files", 1, "*.wav")][ModelDefault("AllowEdit","true")][ReadOnlyProperty()]
        public FileLinkObject File {
            get => _file;
            set => SetPropertyValue(nameof(File), ref _file, value);
        }
        
        [Association("SpeechToText-SpeechTexts")][VisibleInListView(false)]
        public SpeechToText SpeechToText {
            get => _speechToText;
            set => SetPropertyValue(nameof(SpeechToText), ref _speechToText, value);
        }
        
        string _text;

        [Size(SizeAttribute.Unlimited)][VisibleInListView(true)]
        [ModelDefault("AllowEdit","true")][SpellCheck][ImmediatePostData]
        public string Text {
            get => _text;
            set => SetPropertyValue(nameof(Text), ref _text, value);
        }

        
        TimeSpan? _fileDuration;
        [ModelDefault("AllowEdit","false")]
        [ValueConverter(typeof(TimeSpanTicksValueConverter))]
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss_fff)]
        public TimeSpan? FileDuration {
            get => _fileDuration;
            set => SetPropertyValue(nameof(FileDuration), ref _fileDuration, value);
        }

        SpeechLanguage IAudioFileLink.Language => this.Language();

        TimeSpan? _voiceDuration;
        [ModelDefault("AllowEdit","false")]
        [ValueConverter(typeof(TimeSpanTicksValueConverter))]
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss_fff)]
        [ToolTip("Say it as text to calculate the voice duration.")]
        public TimeSpan? VoiceDuration {
            get => _voiceDuration;
            set => SetPropertyValue(nameof(VoiceDuration), ref _voiceDuration, value);
        }

        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss_fff)]
        public TimeSpan End => Duration.Add(Start);
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss_fff)]
        public TimeSpan WaitTime => this.WaitTime();
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss_fff)]
        public TimeSpan VoiceOverTime => this.VoiceOverTime();
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss_fff)]
        public TimeSpan SpareTime => this.SpareTime();

        [ModelDefault("RowCount","5")]
        public string History {
            get {
                return Previous.FromHierarchy(text => text.Previous,text => text!=null).Reverse()
                    .Select(text => text.Text.TrimEnd('.')).Join($".{Environment.NewLine}");
            }
        }

        TimeSpan _start;
        [VisibleInListView(true)][DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss_fff)]
        [ModelDefault("AllowEdit","false")]
        public TimeSpan Start {
            get => _start;
            set => SetPropertyValue(nameof(Start), ref _start, value);
        }

        public SpeechText Previous => this.PreviousSpeechText();
        public SpeechText Next => this.NextSpeechText();

        TimeSpan _duration;
        [VisibleInListView(true)][ModelDefault("AllowEdit","false")]
        [ValueConverter(typeof(TimeSpanTicksValueConverter))]
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss_fff)]
        public TimeSpan Duration {
            get => _duration;
            set => SetPropertyValue(nameof(Duration), ref _duration, value);
        }

        decimal _rate;

        public decimal Rate {
            get => _rate;
            set => SetPropertyValue(nameof(Rate), ref _rate, value);
        }
        public bool CanConvert =>FileDuration != null && FileDuration.Value != TimeSpan.Zero && Duration.Add(SpareTime).Subtract(FileDuration??TimeSpan.Zero)>=TimeSpan.Zero;
    }

    public interface ISelectInExplorer {
        FileLinkObject File { get; set; }
    }
}