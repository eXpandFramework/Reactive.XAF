using DevExpress.Xpo;

namespace Xpand.Extensions.XAF.Xpo.SessionExtensions {
    public static partial class SessionExtensions {
        public static void Close(this Session session){
            session.Disconnect();
            session.Dispose();
        }

    }
}
