using System;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Xpo.BaseObjects;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    public interface IAudioFileLink:IObjectSpaceLink,IXPReceiveOnChangedFromArbitrarySource {
        string Storage { get;  }
        long Oid { get; }
        FileLinkObject File { get; set; }
        TimeSpan Duration { get; set; }
        TimeSpan? VoiceDuration { get; set; }
        TimeSpan? FileDuration { get; set; }
        
        SpeechLanguage Language { get; }
    }
}