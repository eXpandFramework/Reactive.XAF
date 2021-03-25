using System;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.XAF.Modules.Reactive.Rest {
    public interface IRestAttribute {
        string HttpMethod { get; }
        string RequestUrl { get; }
        bool HandleErrors { get; set; }
        int PollInterval { get; set; }
    }


    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Property,AllowMultiple = true)]
    public class RestOperationAttribute:Attribute, IRestAttribute {
        public static int DefaultPollingInterval=60;
        public Operation Operation { get; }
        public string ActionId { get; }
        public string RequestUrl { get; }
        public bool HandleErrors { get; set; }
        public int PollInterval { get; set; }
        public string HttpMethod { get; }

        public RestOperationAttribute(string httpMethod, string requestUrl):this(httpMethod:httpMethod, requestUrl:requestUrl,operation:Operation.Update ) {
        }  

        public RestOperationAttribute(Operation operation,string requestUrl,string httpMethod=null,int pollInterval=0) {
            Operation = operation;
            RequestUrl = requestUrl;
            HttpMethod = httpMethod;
            PollInterval=pollInterval;
            if (operation == Operation.Get&&PollInterval==0) {
                PollInterval=DefaultPollingInterval;
            }
        }
        public RestOperationAttribute(string actionId,string requestUrl,string httpMethod=null,int pollInterval=0) {
            ActionId = actionId;
            RequestUrl = requestUrl;
            HttpMethod = httpMethod;
            if (httpMethod==nameof(System.Net.Http.HttpMethod.Get)&&pollInterval==0) {
                PollInterval=DefaultPollingInterval;
            }
        }

        public string Criteria { get; set; }

    }
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Property,AllowMultiple = true)]
    public class RestActionOperationAttribute:Attribute, IRestAttribute {
        public string ActionId { get; }
        public string RequestUrl { get; }
        public bool HandleErrors { get; set; }
        int IRestAttribute.PollInterval { get; set; }
        public string HttpMethod { get; }

        public RestActionOperationAttribute(string httpMethod,string requestUrl,string actionId=null):this(requestUrl,actionId) => HttpMethod = httpMethod;

        public RestActionOperationAttribute(string requestUrl,string actionId=null) {
            RequestUrl = requestUrl;
            actionId ??= requestUrl.Substring(requestUrl.LastIndexOf("/", StringComparison.Ordinal) + 1).CompoundName();
            ActionId = actionId;
            HttpMethod = nameof(System.Net.Http.HttpMethod.Post);
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class RestPropertyAttribute:Attribute,IRestAttribute {
        public RestPropertyAttribute(string propertyName) => PropertyName = propertyName;
        public RestPropertyAttribute(string httpMethod, string requestUrl) {
            HttpMethod = httpMethod;
            RequestUrl = requestUrl;
            if (httpMethod==nameof(System.Net.Http.HttpMethod.Get)) {
                PollInterval=RestOperationAttribute.DefaultPollingInterval;
            }
        }

        public string PropertyName { get; }
        public bool HandleErrors { get; set; }
        public int PollInterval { get; set; }
        public string HttpMethod { get; }
        public string RequestUrl { get; }
        public override string ToString() => PropertyName??RequestUrl??base.ToString();
    }
    public enum Operation {
        Delete,Get,
        Create,
        Update
    }
}