using System;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.XAF.Attributes.Custom;
using Xpand.Extensions.XAF.NonPersistentObjects;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [DomainComponent]
    public class SpeechTextInfo : NonPersistentBaseObject {
        string _speechType;

        public string SpeechType {
            get => _speechType;
            set => SetPropertyValue(nameof(SpeechType), ref _speechType, value);
        }
        int _selectedLines;

        public int SelectedLines {
            get => _selectedLines;
            set => SetPropertyValue(nameof(SelectedLines), ref _selectedLines, value);
        }

        int _totalLines;

        public int TotalLines {
            get => _totalLines;
            set => SetPropertyValue(nameof(TotalLines), ref _totalLines, value);
        }

        TimeSpan _duration;
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss)]
        public TimeSpan Duration {
            get => _duration;
            set => SetPropertyValue(nameof(Duration), ref _duration, value);
        }

        TimeSpan _audioDuration;
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss)]
        public TimeSpan AudioDuration {
            get => _audioDuration;
            set => SetPropertyValue(nameof(AudioDuration), ref _audioDuration, value);
        }
        
        TimeSpan _ssmlDuration;
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss)]
        public TimeSpan SSMLDuration {
            get => _ssmlDuration;
            set => SetPropertyValue(nameof(SSMLDuration), ref _ssmlDuration, value);
        }

        TimeSpan _ssmlAudioDuration;
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss)]
        public TimeSpan SSMLAudioDuration {
            get => _ssmlAudioDuration;
            set => SetPropertyValue(nameof(SSMLAudioDuration), ref _ssmlAudioDuration, value);
        }

        // TimeSpan _spareTime;

        
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss)]
        public TimeSpan SpareTime => SSMLDuration.Subtract(Duration);

        // set => SetPropertyValue(nameof(SpareTime), ref _spareTime, value);
        TimeSpan _overTime;
        [DisplayDateAndTime(DisplayDateType.None,DisplayTimeType.mm_ss)]
        public TimeSpan OverTime {
            get => _overTime;
            set => SetPropertyValue(nameof(OverTime), ref _overTime, value);
        }
    }
}