using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using HarmonyLib;
using Swordfish.NET.Collections.Auxiliary;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System.IO;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.StoreToDisk{
    public static class StoreToDiskService{
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenSetupComplete()
                .SelectMany(_ =>application.StoreToDisk(application.Model.ToReactiveModule<IModelReactiveModulesStoreToDisk>().StoreToDisk.Folder)
                    .MergeToUnit(application.DailyBackup()) )
                .ToUnit());
        

        private static IObservable<Unit> DailyBackup(this XafApplication application)
            => application.WhenLoggedOn()
                .Select(_ => application.Model.ToReactiveModule<IModelReactiveModulesStoreToDisk>().StoreToDisk)
                .Where(storeToDisk => storeToDisk.DailyBackup).ObserveOnDefault()
                .SelectMany(disk => {
                    var jsonFles = Directory.GetFiles(disk.Folder, "*.json");
                    var directoryName = $"{disk.Folder}\\{DateTime.Now:yyyy.MM.dd}";
                    if (jsonFles.Any() && !Directory.Exists(directoryName)) {
                        Directory.CreateDirectory(directoryName);
                        return jsonFles.Execute(file => File.Copy(file, Path.Combine(directoryName,Path.GetFileName(file)))).ToNowObservable();
                    }
                    return Observable.Empty<string>();
                })
                .ToUnit();

        private static IObservable<(object[] objects, JsonArray JsonArray)> LoadFromDisk(this (object instance,ObjectModification modification)[] source, XafApplication application,
            IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath, StoreToDiskAttribute attribute) 
            => Observable.Defer(() => new FileInfo(filePath).WhenFileReadAsBytes()
                .Select(bytes => bytes.Length == 0 ? new JsonArray() : attribute.Protection.UnProtect(bytes).DeserializeJsonNode().ToJsonArray())
                .SelectMany(jsonArray => application.UseProviderObjectSpace(space => source
                    .LoadFromDisk( keyMember, memberInfos, source.Where(details => details.modification == ObjectModification.New)
                        .Select(t => t.instance).Select(space.GetObject).ToArray(), jsonArray, space),typeInfo.Type)));

        private static IObservable<(object[], JsonArray JsonArray)> LoadFromDisk(this (object instance, ObjectModification modification)[] source, IMemberInfo keyMember, IMemberInfo[] memberInfos, object[] objects, JsonArray jsonArray, IObjectSpace space) 
            => objects.ToNowObservable().BufferUntilCompleted()
                .SelectMany(objects1 => objects1.ToNowObservable()
                    .SelectMany(o1 => jsonArray.Cast<JsonObject>().Where(jToken => keyMember.Match(jToken, o1)).Take(1).ToNowObservable()
                        .SelectMany(jToken => memberInfos.ToNowObservable().WhenDefault(info => info.GetValue(o1))
                            .Do(info => {
                                var value = ((JsonValue)jToken[info.Name])?.Deserialize(!info.MemberTypeInfo.IsPersistent?info.MemberType:info.MemberTypeInfo.KeyMember.MemberType);
                                var change = !info.MemberTypeInfo.IsPersistent ? value :
                                    value == null ? null : space.GetObjectByKey(info.MemberType, value);
                                info.SetValue(o1, change);
                            })
                            .Select(_ => o1)))
                )
                .BufferUntilCompleted()
                .Select(_ => {
                    space.CommitChanges();
                    return (source.Where(t => t.modification!=ObjectModification.New)
                        .Select(t => space.GetObject(t.instance)).Concat(objects).ToArray(), JsonArray: jsonArray);
                });

        public static IObservable<Unit> StoreToDisk(this XafApplication application, string directory)
            => application.StoreToDiskData(directory)
                .SelectMany(data => application.WhenProviderCommittedDetailed(data.typeInfo.Type,ObjectModification.NewOrUpdated,emitUpdatingObjectSpace:true)
                    .SelectMany(committed => committed.details.ToArray()
                        .LoadFromDisk(application, data.keyMember, data.memberInfos, data.typeInfo, data.filePath, data.attribute)
                        .StoreToDisk(committed,data)))
                .ToUnit();
        
        private static IObservable<JsonObject[]> StoreToDisk(this IObservable<(object[] objects, JsonArray JsonArray)> source,
                (IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details) committed,
                (IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath, StoreToDiskAttribute attribute) data)
            => source.SelectMany(loadFromDisk => data.ObjectsToStore(committed,  loadFromDisk.objects,loadFromDisk.objects.Select(o => data.keyMember.GetValue(o)).ToHashSet())
                .StoreToDisk(loadFromDisk.JsonArray, data.keyMember, data.memberInfos, data.filePath, data.attribute));

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
        
        private static IObservable<JsonObject[]> StoreToDisk(this object[] objects, JsonArray jsonArray,
            IMemberInfo keyMember, IMemberInfo[] memberInfos, string filePath, StoreToDiskAttribute attribute) 
            => objects.ToNowObservable().SelectMany(instance => {
                var jtoken = memberInfos.GetObject(keyMember,   instance);
                return memberInfos.ToNowObservable()
                    .Do(memberInfo => jtoken[memberInfo.Name] = memberInfo.GetMemberValue(instance).ToJsonNode())
                    .ConcatIgnoredValue(jtoken);
            }).BufferUntilCompleted(true)
                .Do(jsonObjects => new JsonArray(jsonObjects.Concat(objects.ReplaceExisting(jsonArray, keyMember)
                            .Select(node => node.Deserialize<JsonObject>())).Cast<JsonNode>().ToArray())
                    .SaveFile(attribute.Protection, filePath))
                .TraceStoreToDisk();

        private static JsonArray ReplaceExisting(this object[] objects,JsonArray jsonArray  ,IMemberInfo keyMember) {
            objects.ForEach(o => jsonArray.Cast<JsonObject>().Where(token => keyMember.Match(token, o)).Take(1).ToArray()
                .ForEach(token => jsonArray.Remove(token)));
            return jsonArray;
        }

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        private static void SaveFile(this JsonArray json,DataProtectionScope? scope,string filePath) {
            var bytes = json.Utf8Bytes(new JsonSerializerOptions(){WriteIndented = true});
            (scope != null ? bytes.Protect(scope.Value) : bytes).Save(filePath);
        }

        private static JsonObject GetObject(this IMemberInfo[] memberInfos,IMemberInfo keyMember,   object instance) 
            => memberInfos.Select(info => (info.Name,info.GetMemberValue(instance)))
                .AddItem((keyMember.Name, keyMember.GetMemberValue(instance))).ToDictionary()
                .SerializeToNode()!.AsObject();

        private static bool Match(this IMemberInfo memberInfo, JsonObject token, object instance) {
            var value = token[memberInfo.Name]?.Deserialize(memberInfo.MemberType);
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

        internal static IObservable<TSource> TraceStoreToDisk<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, StoreToDiskModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

    }
}