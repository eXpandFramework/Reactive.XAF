﻿using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
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
using Xpand.Extensions.Reactive.Transform.System.IO;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.StoreToDisk{
    public static class StoreToDiskService{
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenSetupComplete()
                .SelectMany(_ =>application.StoreToDisk(application.Model.ToReactiveModule<IModelReactiveModulesStoreToDisk>().StoreToDisk.Folder) )
                .ToUnit());

        
        private static IObservable<Unit> LoadFromDisk(this IObservable<(IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath,
                StoreToDiskAttribute attribute)> source, XafApplication application)
            => source.SelectMany(t => application.WhenProviderCommittedDetailed(t.typeInfo.Type,ObjectModification.New,true)
                    .Select(tv => tv.details.Select(t2 => t2.instance).ToArray())
                    .SelectManySequential(t2 => new FileInfo(t.filePath).WhenFileReadAsBytes()
                        .WhenNotDefault(bytes => bytes.Length)
                        .Select(bytes => t.attribute.Protection.UnProtect(bytes))
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .SelectMany(json=>json.DeserializeJson())
                        .Select(token => (token, keyValue: token[t.keyMember.Name]))
                        .SelectMany(t3 => application.UseProviderObjectSpace(space => t2.Select(space.GetObject).ToNowObservable().BufferUntilCompleted()
                            .SelectMany(objects =>objects.Where(o => t.keyMember.Match(t3.token,o)).Take(1).ToNowObservable()
                                .SelectMany(o => t.memberInfos.ToNowObservable().WhenDefault(info => info.GetValue(o))
                                    .Do(info => info.SetValue(o, ((string)t3.token[info.Name]).Change(info.MemberType))))
                                .TraceStoreToDisk()
                                .IgnoreElements().ConcatToUnit(Observable.Defer(space.Commit))),t.typeInfo.Type)) ))
                .ToUnit();
        

        public static IObservable<Unit> StoreToDisk(this XafApplication application, string directory) 
            => application.StoreToDiskData(directory).Publish(source => source.LoadFromDisk( application)
                    .MergeToUnit(source.SelectMany(t=>application.StoreToDisk(t.keyMember, t.memberInfos, t.typeInfo, t.filePath,t.attribute))));
        
        private static IObservable<(IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath, StoreToDiskAttribute attribute)> StoreToDiskData(
                this XafApplication application, string directory)
            => application.TypesInfo.PersistentTypes.Attributed<StoreToDiskAttribute>().ToNowObservable()
                .Select(t => (keyMember:t.typeInfo.FindMember(t.attribute.Key),memberInfos:t.attribute.Properties.Select(property =>t.typeInfo.FindMember(property)).ToArray(),t.attribute,t.typeInfo))
                .SelectMany(t => new DirectoryInfo(directory).WhenDirectory().Select(_ => t.typeInfo.EnsureFile(directory))
                    .Select(filePath => ( t.keyMember, t.memberInfos, t.typeInfo, filePath,t.attribute)));

        private static IObservable<IMemberInfo> StoreToDisk(this XafApplication application, IMemberInfo keyMember,
            IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath, StoreToDiskAttribute attribute)
            => application.WhenProviderCommittedDetailed(typeInfo.Type, ObjectModification.NewOrUpdated,true, modifiedProperties: memberInfos.Select(info => info.Name).ToArray())
                .BufferUntilInactive(TimeSpan.FromSeconds(1)).Select(list => list.SelectMany(t1 => t1.details.Select(t2 => t2.instance)).ToArray())
                .SelectManySequential(objects => new FileInfo(filePath).WhenFileReadAsBytes()
                    .Select(bytes => bytes.Length == 0 ? "[]" : attribute.Protection.UnProtect(bytes))
                    .Select(text => text.DeserializeJson())
                    .SelectMany(deserializeJson => objects.ToNowObservable().SelectMany(instance => {
                        var jtoken = deserializeJson.GetToken(keyMember, memberInfos,  instance);
                        return memberInfos.ToNowObservable().Do(memberInfo =>
                                jtoken[memberInfo.Name] = memberInfo.GetValue(instance).ToJToken())
                            .FinallySafe(() => {
                                var json = new JArray(deserializeJson.ToArray().Where(token => !keyMember.Match(token, instance)).ToArray()
                                    .AddItem(jtoken)).ToString();
                                if (attribute.Protection != null) {
                                    json.Protect(attribute.Protection.Value).Save(filePath);
                                }
                                else {
                                    json.Bytes().Save(filePath);
                                }
                            })
                            .TraceStoreToDisk();
                    })));

        private static JToken GetToken(this JToken deserializeJson,IMemberInfo keyMember, IMemberInfo[] memberInfos,  object instance) 
            => deserializeJson.FirstOrDefault(token => keyMember.Match(token, instance)) ??
               JObject.FromObject(memberInfos.NameValues(instance)
                   .AddItem((keyMember.Name, keyMember.GetValue(instance).ToJToken())).ToDictionary());

        private static bool Match(this IMemberInfo keyMember, JToken token, object instance) 
            => token[keyMember.Name].ToObject(keyMember.MemberType)!.Equals(keyMember.GetValue(instance));

        private static string EnsureFile(this  ITypeInfo typeInfo,string directory){
            var filePath = $"{new DirectoryInfo(directory).FullName}\\{typeInfo.Type.StoreToDiskFileName()}";
            if (!File.Exists(filePath)){
                File.CreateText(filePath);
            }
            return filePath;
        }

        public static string StoreToDiskFileName(this Type type) => $"{type.FullName.CleanCodeName()}.json";

        internal static IObservable<TSource> TraceStoreToDisk<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, StoreToDiskModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

    }
}