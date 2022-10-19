using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Attributes.Custom;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.Speech.Services;
using Xpand.XAF.Modules.SpellChecker;
using Xpand.XAF.Modules.ViewItemValue;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [NavigationItem("Speech")][DefaultClassOptions][OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    [FileAttachment(nameof(File))][DeferredDeletion(false)][ImageName("XafBarLinkContainerItem")]
    [SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types")]
    [DefaultProperty(nameof(Text))]
    [CloneModelView(CloneViewType.DetailView, TypeSpeakDetailView)]
    public class TextToSpeech:CustomBaseObject, IAudioFileLink {
        public const string TypeSpeakDetailView = nameof(TextToSpeech) + "_TypeSpeak_DetailView";
        public TextToSpeech(Session session) : base(session) { }
        FileLinkObject _file;
        
        [ModelDefault("AllowEdit","false")]
        [FileTypeFilter("Audio files", 1, "*.wav")]
        public FileLinkObject File {
            get => _file;
            set => SetPropertyValue(nameof(File), ref _file, value);
        }

        string _storage;

        [ViewItemValue()]
        public string Storage {
            get => _storage;
            set => SetPropertyValue(nameof(Storage), ref _storage, value);
        }
        string _text;

        [Size(-1)][SpellCheck][ImmediatePostData]
        public string Text {
            get => _text;
            set => SetPropertyValue(nameof(Text), ref _text, value);
        }

        TimeSpan _duration;
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss)][ReadOnlyProperty()]
        public TimeSpan Duration {
            get => _duration;
            set => SetPropertyValue(nameof(Duration), ref _duration, value);
        }

        [Size(-1)]
        public string BeforeText {
            get {
                var oid = Oid;
                if (oid == 0) {
                    oid = Session.Query<TextToSpeech>().Max(speech => speech.Oid)+1;
                }
                
                return Session.Query<TextToSpeech>().Where(speech => speech.Oid<oid&&speech.Text!=null).OrderByDescending(speech => speech.Oid)
                    .Take(5).Select(speech => speech._text).ToArray().Reverse().Join($"{Environment.NewLine}{Environment.NewLine}");
            }
        }

        TimeSpan? IAudioFileLink.VoiceDuration {
            get => null;
            set { }
        }

        TimeSpan? _fileDuration;
        [ReadOnlyProperty()]
        public TimeSpan? FileDuration {
            get => _fileDuration;
            set => SetPropertyValue(nameof(FileDuration), ref _fileDuration, value);
        }

        SpeechLanguage _language;

        [RuleRequiredField][ViewItemValue()]
        [DataSourceProperty(nameof(Languages))]
        public SpeechLanguage Language {
            get => _language;
            set => SetPropertyValue(nameof(Language), ref _language, value);
        }

        public List<SpeechLanguage> Languages => ObjectSpace
            .DefaultSpeechAccount(CaptionHelper.Instance.ApplicationModel.SpeechModel()).Languages;
    }
}