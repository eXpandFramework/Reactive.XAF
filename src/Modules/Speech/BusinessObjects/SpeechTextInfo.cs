using System;
using DevExpress.ExpressApp.DC;
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

        public TimeSpan Duration {
            get => _duration;
            set => SetPropertyValue(nameof(Duration), ref _duration, value);
        }

        TimeSpan _ssmlDuration;

        public TimeSpan SSMLDuration {
            get => _ssmlDuration;
            set => SetPropertyValue(nameof(SSMLDuration), ref _ssmlDuration, value);
        }
    }
}