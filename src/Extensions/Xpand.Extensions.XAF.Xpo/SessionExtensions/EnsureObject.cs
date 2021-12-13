using System;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.Xpo;
using Fasterflect;

namespace Xpand.Extensions.XAF.Xpo.SessionExtensions {
    public static partial class SessionExtensions {
        public static T EnsureObject<T>(this Session session, Expression<Func<T, bool>> criteriaExpression,
            Action<T> initialize,bool inTransaction=false) {
            var query = session.Query<T>();
            if (inTransaction) {
                query = query.InTransaction();
            }
            var ensureObject = query.FirstOrDefault(criteriaExpression);
            if (ensureObject != null) {
                return ensureObject;
            }

            ensureObject = (T)typeof(T).CreateInstance(session);
            initialize(ensureObject);
            return ensureObject;
        }
    }
}