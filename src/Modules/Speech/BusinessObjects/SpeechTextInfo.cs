using System;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Attributes.Custom;
using Xpand.Extensions.XAF.NonPersistentObjects;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [DomainComponent]
    public class SpeechTextInfo : NonPersistentBaseObject {
        string _speechType;

        [XafDisplayName("Type")]
        public string SpeechType {
            get => _speechType;
            set => SetPropertyValue(nameof(SpeechType), ref _speechType, value);
        }
        int _selectedLines;

        [XafDisplayName("Selection")]
        public int SelectedLines {
            get => _selectedLines;
            set => SetPropertyValue(nameof(SelectedLines), ref _selectedLines, value);
        }

        int _totalLines;

        [XafDisplayName("Total")]
        public int TotalLines {
            get => _totalLines;
            set => SetPropertyValue(nameof(TotalLines), ref _totalLines, value);
        }

        TimeSpan _duration;
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss)]
        [ToolTip("Duration Sum without breaks")]
        public TimeSpan Duration {
            get => _duration;
            set => SetPropertyValue(nameof(Duration), ref _duration, value);
        }

        TimeSpan _voiceDuration;
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss)]
        [ToolTip("Voice duration Sum without breaks")]
        [XafDisplayName("Voice")]
        public TimeSpan VoiceDuration {
            get => _voiceDuration;
            set => SetPropertyValue(nameof(VoiceDuration), ref _voiceDuration, value);
        }
        
        TimeSpan _ssmlDuration;
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss)]
        [ToolTip("SSML duration Sum")]
        [XafDisplayName("SSML")]
        public TimeSpan SSMLDuration {
            get => _ssmlDuration;
            set => SetPropertyValue(nameof(SSMLDuration), ref _ssmlDuration, value);
        }

        TimeSpan _ssmlVoiceDuration;
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss)]
        [ToolTip("SSML voice duration Sum")]
        [XafDisplayName("SSMLVoice")]
        public TimeSpan SSMLVoiceDuration {
            get => _ssmlVoiceDuration;
            set => SetPropertyValue(nameof(SSMLVoiceDuration), ref _ssmlVoiceDuration, value);
        }

        // TimeSpan _spareTime;

        
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss)]
        [XafDisplayName("Spare")]
        public TimeSpan SpareTime => SSMLDuration.Subtract(Duration);

        // set => SetPropertyValue(nameof(SpareTime), ref _spareTime, value);
        TimeSpan _overTime;
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss)]
        [ToolTip("Voice minus duration sum")]
        [XafDisplayName("Over")]
        public TimeSpan OverTime {
            get => _overTime;
            set => SetPropertyValue(nameof(OverTime), ref _overTime, value);
        }
    }
}