using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using Fasterflect;

namespace Xpand.Extensions.XAF.SecurityExtensions {
    public static partial class SecurityExtensions {
        public static T GetCurrentUser<T>(this XafApplication application) => (T) application.Security.User;
        public static T GetCurrentUser<T>(this ISecurityStrategyBase security) => (T) security.User;

        public static ISecurityUserWithRoles GetUser(this IObjectSpace objectSpace, string userName,
            string passWord = "", params ISecurityRole[] roles) 
            => (ISecurityUserWithRoles) objectSpace.FindObject(SecuritySystem.UserType, new BinaryOperator("UserName", userName)) ??
               CreateUser(objectSpace, userName, passWord, roles);

        public static ISecurityUserWithRoles CreateUser(this IObjectSpace objectSpace, string userName, string passWord, ISecurityRole[] roles) {
            var user2 = (ISecurityUserWithRoles)objectSpace.CreateObject(SecuritySystem.UserType);
            var typeInfo = objectSpace.TypesInfo.FindTypeInfo(user2.GetType());
            typeInfo.FindMember("UserName").SetValue(user2, userName);
            user2.CallMethod("SetPassword",new[]{typeof(string)}, passWord);
            var roleCollection = typeInfo.FindMember("Roles").GetValue(user2);
            foreach (var role in roles) {
                roleCollection.CallMethod("BaseAdd",role);
            }
            return user2;
        }

    }
}