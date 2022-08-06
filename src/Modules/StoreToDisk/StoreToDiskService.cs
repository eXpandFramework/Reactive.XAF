using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Swordfish.NET.Collections.Auxiliary;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
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

        private static IObservable<(object[] objects, JArray jArray)> LoadFromDisk(this object[] objects, XafApplication application,
            IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath, StoreToDiskAttribute attribute) 
            => Observable.Defer(() => new FileInfo(filePath).WhenFileReadAsBytes()
                .Select(bytes => bytes.Length == 0 ? "[]" : attribute.Protection.UnProtect(bytes)).Select(json=>json.DeserializeJson().As<JArray>())
                .SelectMany(jArray => application.UseProviderObjectSpace(space => objects.Select(space.GetObject).ToNowObservable().BufferUntilCompleted()
                    .SelectMany(objects1 =>objects1.ToNowObservable().SelectMany(o1 => jArray.Where(jToken => keyMember.Match(jToken,o1)).Take(1).ToNowObservable()
                        .SelectMany(jToken => memberInfos.ToNowObservable().WhenDefault(info => info.GetValue(o1))
                            .Do(info => info.SetValue(o1, ((string)jToken[info.Name]).Change(info.MemberType)))
                            .Select(_ => o1).Distinct()))
                    )
                    .BufferUntilCompleted()
                    .Select(objects1 => {
                        space.CommitChanges();
                        return (objects1,jArray);
                    }),typeInfo.Type)));
        
        public static IObservable<Unit> StoreToDisk(this XafApplication application, string directory)
            => application.StoreToDiskData(directory)
                .SelectMany(data => application.WhenProviderCommittedDetailed(data.typeInfo.Type,ObjectModification.NewOrUpdated,emitUpdatingObjectSpace:true)
                    .SelectMany(committed => committed.details.Where(details =>details.modification==ObjectModification.New ).Select(newObjects => newObjects.instance).ToArray()
                        .LoadFromDisk(application, data.keyMember, data.memberInfos, data.typeInfo, data.filePath, data.attribute)
                        .StoreToDisk(committed,data)))
                .ToUnit();
        
        private static IObservable<JObject[]> StoreToDisk(this IObservable<(object[] objects, JArray jArray)> source,
                (IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details) committed,
                (IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath, StoreToDiskAttribute attribute) data)
            => source.SelectMany(loadFromDisk => data.ObjectsToStore(committed,  loadFromDisk.objects,loadFromDisk.objects.Select(o => data.keyMember.GetValue(o)).ToHashSet())
                .StoreToDisk(loadFromDisk.jArray,data.keyMember, data.memberInfos, data.filePath, data.attribute));

        private static object[] ObjectsToStore(this (IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath, StoreToDiskAttribute attribute) data,
            (IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details) committed, object [] loadedObjects, HashSet<object> loadedKeys) 
            => committed.details.Where(details => !loadedKeys.Contains(data.keyMember.GetValue(details.instance)))
                .Select(updated => updated.instance).Concat(loadedObjects).ToArray();

        private static IObservable<(IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath, StoreToDiskAttribute attribute)> StoreToDiskData(
                this XafApplication application, string directory)
            => application.TypesInfo.PersistentTypes.Attributed<StoreToDiskAttribute>().ToNowObservable()
                .Select(t => (keyMember:t.typeInfo.FindMember(t.attribute.Key),memberInfos:t.attribute.Properties.Select(property =>t.typeInfo.FindMember(property)).ToArray(),t.attribute,t.typeInfo))
                .SelectMany(t => new DirectoryInfo(directory).WhenDirectory().Select(_ => t.typeInfo.EnsureFile(directory))
                    .Select(filePath => ( t.keyMember, t.memberInfos, t.typeInfo, filePath,t.attribute)));
        
        private static IObservable<JObject[]> StoreToDisk(this object[] objects ,JArray jArray,IMemberInfo keyMember, IMemberInfo[] memberInfos, string filePath, StoreToDiskAttribute attribute) 
            => objects.ToNowObservable().SelectMany(instance => {
                var jtoken = jArray.GetToken(keyMember, memberInfos,  instance);
                return memberInfos.ToNowObservable().Do(memberInfo =>
                        jtoken[memberInfo.Name] = memberInfo.GetValue(instance).ToJToken())
                    .ConcatIgnoredValue(jtoken);
            }).BufferUntilCompleted(true)
                .Do(jObjects => attribute.SaveFile(filePath,  new JArray(jObjects.Concat(objects.ReplaceExisting(jArray, keyMember))).ToString()))
                .TraceStoreToDisk();

        private static JArray ReplaceExisting(this object[] objects,JArray jArray  ,IMemberInfo keyMember) {
            objects.ForEach(o => jArray.Where(token => keyMember.Match(token, o)).Take(1).ToArray()
                    .ForEach(token => jArray.Remove(token)));
            return jArray;
        }

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        private static void SaveFile(this StoreToDiskAttribute attribute,string filePath,  string json) 
            => (attribute.Protection != null ? json.Protect(attribute.Protection.Value) : json.Bytes()).Save(filePath);

        private static JObject GetToken(this JArray jArray,IMemberInfo keyMember, IMemberInfo[] memberInfos,  object instance) 
            => jArray.Cast<JObject>().FirstOrDefault(token => keyMember.Match(token, instance)) ??
               JObject.FromObject(memberInfos.NameValues(instance)
                   .AddItem((keyMember.Name, keyMember.GetValue(instance).ToJToken())).ToDictionary());

        private static bool Match(this IMemberInfo memberInfo, JToken token, object instance) {
            var value = token[memberInfo.Name].ToObject(memberInfo.MemberType);
            var keyValue = memberInfo.GetValue(instance);
            return value == null ? keyValue == null : value!.Equals(keyValue);
        }

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