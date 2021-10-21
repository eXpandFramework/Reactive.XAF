using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.TestsLib.Net461;

// ReSharper disable once CheckNamespace
namespace Xpand.XAF.Modules.Office.Cloud.Tests{
    public abstract class CloudServiceTests<TAuthentication>:BaseTest where TAuthentication:CloudOfficeBaseObject{
        protected abstract string ServiceName{ get; }

        protected abstract void NewAuthentication(Platform platform, XafApplication application);

        protected  abstract IObservable<bool> NeedsAuthentication(XafApplication application);

        protected abstract XafApplication Application(Platform platform);


        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public void Actions_are_Activated_For_CurrentUser_Details(){
            using var application=Application(Platform.Win);
            application.Actions_are_Activated_For_CurrentUser_Details(ServiceName);
        }

        protected abstract void OnConnect_Action_Creates_Connection(Platform platform, XafApplication application);
        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public async Task Not_NeedsAuthentication_when_Authentication_current_user_can_authenticate(){
            using var application=Application(Platform.Win);
            NewAuthentication(Platform.Win, application);
        
            await NeedsAuthentication(application).Select(b => b).Not_NeedsAuthentication_when_AuthenticationStorage_current_user_can_authenticate();
        }
        
        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public async Task NeedsAuthentication_when_AuthenticationStorage_does_not_contain_current_user(){
            using var application=Application(Platform.Win);
            await application.NeedsAuthentication_when_AuthenticationStorage_does_not_contain_current_user(() => NeedsAuthentication(application));
        }

        [Test][XpandTest()][Apartment(ApartmentState.STA)]
        public async Task Actions_Active_State_when_authentication_not_needed(){
            using var application=Application(Platform.Win);
            NewAuthentication(Platform.Win, application);
            await application.Actions_Active_State_when_authentication_not_needed(ServiceName);
        }
        
        [Test]
        [XpandTest()][Apartment(ApartmentState.STA)]
        public async Task Disconnect_Action_Destroys_Connection(){
            using var application=Application(Platform.Win);
            NewAuthentication(Platform.Win, application);
            await application.Disconnect_Action_Destroys_Connection(ServiceName);
        }

    }
}