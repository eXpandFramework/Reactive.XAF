using DevExpress.Xpo;

namespace Xpand.Extensions.XAF.Xpo.SessionExtensions {
    public static class SessionExtensions {
        public static void Close(this Session session){
            session.Disconnect();
            session.Dispose();
        }

    }
}
