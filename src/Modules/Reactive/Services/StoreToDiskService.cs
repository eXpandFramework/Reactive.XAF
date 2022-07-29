using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class StoreToDiskService{
        private static IObservable<Unit> LoadFromDisk(this IObservable<(IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath,StoreToDiskAttribute attribute)> source, XafApplication application)
            => source.SelectMany(t => application.WhenProviderCommittedDetailed(t.typeInfo.Type,t.attribute.ObjectModification,true)
                    .Select(tv => tv.details.Select(t2 => t2.instance).Where(o => tv.objectSpace.IsObjectFitForCriteria(CriteriaOperator.Parse(t.attribute.Criteria),o)).ToArray())
                    .SelectManySequential(t2 => AppDomain.CurrentDomain.WhenFileReadAsString(t.filePath).Where(s => !string.IsNullOrWhiteSpace(s))
                        .SelectMany(json=>json.DeserializeJson())
                        .Select(token => (token, keyValue: token[t.keyMember.Name]))
                        .SelectMany(t3 => application.UseProviderObjectSpace(space => t2.Select(space.GetObject).ToNowObservable().BufferUntilCompleted()
                            .SelectMany(objects =>objects.Where(o => t.keyMember.Match(t3.token,o)).Take(1).ToNowObservable()
                                .SelectMany(o => t.memberInfos.ToNowObservable().WhenDefault(info => info.GetValue(o))
                                    .Do(info => info.SetValue(o, ((string)t3.token[info.Name]).Change(info.MemberType))))
                                .IgnoreElements().ConcatToUnit(Observable.Defer(space.Commit))),t.typeInfo.Type)) ))
                .ToUnit();

        public static IObservable<Unit> StoreToDisk(this XafApplication application, string directory) 
            => application.StoreToDiskData(directory).Publish(source => source.LoadFromDisk( application)
                    .MergeToUnit(source.SelectMany(t=>application.StoreToDisk(t.keyMember, t.memberInfos, t.typeInfo, t.filePath))));

        private static IObservable<(IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath,StoreToDiskAttribute attribute)> StoreToDiskData(this XafApplication application, string directory)
            => application.TypesInfo.PersistentTypes.Attributed<StoreToDiskAttribute>().ToNowObservable()
                .Select(t => (keyMember:t.typeInfo.FindMember(t.attribute.Key),memberInfos:t.attribute.Properties.Select(property =>t.typeInfo.FindMember(property)).ToArray(),t.attribute,t.typeInfo))
                .SelectMany(t => AppDomain.CurrentDomain.WhenDirectory(directory).Select(_ => t.typeInfo.EnsureFile(directory))
                    .Select(filePath => ( t.keyMember, t.memberInfos, t.typeInfo, filePath,t.attribute)));

        private static IObservable<IMemberInfo> StoreToDisk(this XafApplication application, IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath)
            => application.WhenProviderCommittedDetailed(typeInfo.Type, ObjectModification.Updated, modifiedProperties: memberInfos.Select(info => info.Name).ToArray())
                .BufferUntilInactive(TimeSpan.FromSeconds(1)).Select(list => list.SelectMany(t1 => t1.details.Select(t2 => t2.instance)).ToArray())
                .SelectManySequential(objects => AppDomain.CurrentDomain.WhenFileReadAsString(filePath)
                    .Select(s1 => string.IsNullOrEmpty(s1) ? "[]" : s1).Select(text => text.DeserializeJson())
                    .SelectMany(deserializeJson => objects.ToNowObservable().SelectMany(instance => {
                        var jtoken = deserializeJson.FirstOrDefault(token => keyMember.Match( token, instance) ) ??
                                     JObject.FromObject(memberInfos.NameValues(instance).AddItem((keyMember.Name, keyMember.GetValue(instance).ToJToken())).ToDictionary());
                        return memberInfos.ToNowObservable().Do(memberInfo => jtoken[memberInfo.Name] = memberInfo.GetValue(instance).ToJToken())
                            .FinallySafe(() => {
                                var jTokens = deserializeJson.ToArray();
                                var enumerable = jTokens.Where(token => !keyMember.Match(token, instance)).ToArray();
                                new JArray(enumerable.AddItem(jtoken)).ToString().Bytes().Save(filePath);
                            });
                    })));

        private static bool Match(this IMemberInfo keyMember, JToken token, object instance) 
            => token[keyMember.Name].ToObject(keyMember.MemberType)!.Equals(keyMember.GetValue(instance));

        private static string EnsureFile(this  ITypeInfo typeInfo,string directory){
            var filePath = $"{directory}\\{typeInfo.FullName.CleanCodeName()}.json";
            if (!File.Exists(filePath)){
                File.CreateText(filePath);
            }

            return filePath;
        }
    }
}