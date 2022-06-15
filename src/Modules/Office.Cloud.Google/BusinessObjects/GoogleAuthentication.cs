using System.Collections.Generic;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;

using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.Extensions.XAF.Xpo.ValueConverters;

namespace Xpand.XAF.Modules.Office.Cloud.Google.BusinessObjects{
    [DeferredDeletion(false)]
    [OptimisticLocking(false)]
    public class GoogleAuthentication : CloudOfficeBaseObject{
        private Dictionary<string, string> _oAuthToken;

        public GoogleAuthentication(Session session) : base(session){
        }

        [VisibleInDetailView(false)]
        [Size(SizeAttribute.Unlimited)]
        [ValueConverter(typeof(DictionaryValueConverter))]
        public Dictionary<string, string> OAuthToken{
            get => _oAuthToken;
            set => SetPropertyValue(nameof(OAuthToken), ref _oAuthToken, value);
        }

        string _userName;

        public string UserName{
            get => _userName;
            set => SetPropertyValue(nameof(UserName), ref _userName, value);
        }

        public override void AfterConstruction(){
            base.AfterConstruction();
            OAuthToken = new Dictionary<string, string>();
        }

        protected override void OnLoaded(){
            base.OnLoaded();
            OAuthToken ??= new Dictionary<string, string>();
        }
    }
}