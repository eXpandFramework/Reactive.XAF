using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [CloneModelView(CloneViewType.DetailView, nameof(SpeechText)+"_MD_DetailView")]
    [CloneModelView(CloneViewType.ListView,BandedListView)]
    [DefaultProperty(nameof(Text))][DeferredDeletion(false)][OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    [ImageName(("Action_Export_ToText"))][CreatableItem(false)]
    [Appearance("Bold Text",AppearanceItemType.ViewItem, "1=1",TargetItems = nameof(Text),FontStyle = FontStyle.Bold,Context = BandedListView)]
    [FileAttachment(nameof(File))]
    public class SpeechText:CustomBaseObject,ISelectInExplorer {
        public const string BandedListView = nameof(BusinessObjects.SpeechToText) + "_" +
                                              nameof(BusinessObjects.SpeechToText.SpeechTexts) + "_Banded_ListView";
        public SpeechText(Session session) : base(session) { }

        [Association("SpeechText-SSMLFiles")]
        public XPCollection<SSMLFile> SSMLFiles => GetCollection<SSMLFile>();
        
        [Association("SpeechText-Translations")][Aggregated]
        public XPCollection<SpeechTranslation> RealTranslations => GetCollection<SpeechTranslation>();
        [CollectionOperationSet(AllowAdd = false)]
        public BindingList<SpeechTranslation> Translations => SpeechToText.Texts.SelectMany(text => text.RealTranslations).ToBindingList();
        SpeechToText _speechToText;
        FileLinkObject _file;
        [FileTypeFilter("Audio files", 1, "*.wav")][ModelDefault("AllowEdit","true")]
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
        [ModelDefault("AllowEdit","true")]
        public string Text {
            get => _text;
            set => SetPropertyValue(nameof(Text), ref _text, value);
        }

        TimeSpan _audioDuration;
        [ModelDefault("AllowEdit","false")]
        [ValueConverter(typeof(TimeSpanSecondsValueConverter))]
        public TimeSpan AudioDuration {
            get => _audioDuration;
            set => SetPropertyValue(nameof(AudioDuration), ref _audioDuration, value);
        }

        public TimeSpan End => Duration.Add(Start);

        [VisibleInListView(true)]
        public TimeSpan Start => TimeSpan.FromTicks(Offset);

        long _offset;
        [VisibleInListView(true)]
        [ModelDefault("AllowEdit","false")]
        public long Offset {
            get => _offset;
            set => SetPropertyValue(nameof(Offset), ref _offset, value);
        }

        TimeSpan _duration;
        [VisibleInListView(true)][ModelDefault("AllowEdit","false")]
        [ValueConverter(typeof(TimeSpanSecondsValueConverter))]
        public TimeSpan Duration {
            get => _duration;
            set => SetPropertyValue(nameof(Duration), ref _duration, value);
        }

        
    }

    public interface ISelectInExplorer {
        FileLinkObject File { get; set; }
    }
}