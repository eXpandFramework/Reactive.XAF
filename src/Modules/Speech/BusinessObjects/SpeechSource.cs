using System.Diagnostics.CodeAnalysis;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Attributes;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Speech.BusinessObjects {
    [OptimisticLocking(OptimisticLockingBehavior.LockModified)]
    [DeferredDeletion(false)][SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types")]
    public abstract class SpeechSource:CustomBaseObject {
        protected SpeechSource(Session session) : base(session) { }
        [InvisibleInAllViews]
        public bool IsValid => GetIsValid();

        
        public string Name => GetName();

        protected abstract string GetName();

        protected abstract bool GetIsValid();
    }
}