using System.ComponentModel;
using System.Net.Http;
using System.Text.Json.Serialization;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive.Rest.Tests.BO {
    [DomainComponent]
    [RestOperation(Operation.Get, "Get"+nameof(RestPropertyObject) )]
    public class RestPropertyObject : NonPersistentBaseObject {
        string _name;


        public string Name {
            get => _name;
            set => SetPropertyValue(ref _name, value);
        }

        private string[] _stringArray;
        [RestProperty(nameof(StringArrayList))]
        public string[] StringArray {
            get => _stringArray;
            set => SetPropertyValue(ref _stringArray, value);
        }

        [DataSourceProperty(nameof(StringArraySource))]
        public BindingList<ObjectString> StringArrayList { get; private set; }
        [Browsable(false)]
        public ReactiveCollection<ObjectString> StringArraySource => _objectStrings;

        protected override void OnObjectSpaceChanged() {
            base.OnObjectSpaceChanged();
            if (ObjectSpace != null) {
                _objectStrings = new(ObjectSpace) {new ObjectString(nameof(StringArraySource))};    
            }
        }

        RestOperationObject _restOperationObject;
        string _restOperationObjectName;
        private ReactiveCollection<ObjectString> _objectStrings;

        [RestProperty(nameof(RestOperationObject))]
        public string RestOperationObjectName {
            get => _restOperationObjectName;
            set => SetPropertyValue(ref _restOperationObjectName, value);
        }

        public RestOperationObject RestOperationObject {
            get => _restOperationObject;
            set => SetPropertyValue(ref _restOperationObject, value);
        }

        [RestProperty(nameof(HttpMethod.Get), "Get" +nameof(RestObjectStats)+ "?id={" +nameof(Name)+ "}")]
        [ExpandObjectMembers(ExpandObjectMembers.Always)]
        [JsonIgnore]
        public RestObjectStats RestObjectStats { get; protected set; }

        [RestProperty(nameof(HttpMethod.Get), nameof(ActiveObjects),HandleErrors=true)]
        [JsonIgnore]
        public ReactiveCollection<RestActiveObject> ActiveObjects { get; protected set; }
    }

    public class RestActiveObject:NonPersistentBaseObject {
        
    }
    public class RestObjectStats:NonPersistentBaseObject {
        string _name;

        public string Name {
            get => _name;
            set => SetPropertyValue(ref _name, value);
        }
    }
}