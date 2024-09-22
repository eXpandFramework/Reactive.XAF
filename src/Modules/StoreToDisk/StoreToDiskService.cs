using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
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
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System.IO;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.StoreToDisk{
    public static class StoreToDiskService{
        private static readonly ConcurrentDictionary<string, Dictionary<object,JsonNode>> StorageCache = new();
        private static readonly ConcurrentDictionary<Type, string> StoreToDiskFileNames = new();
        internal static IObservable<Unit> Connect(this XafApplication application) 
            => application.WhenSetupComplete()
                .SelectMany(_ =>application.StoreToDisk(application.Model.ToReactiveModule<IModelReactiveModulesStoreToDisk>().StoreToDisk.Folder)
                    .MergeToUnit(application.DailyBackup()) );

        public static void ClearCache(this StoreToDiskModule module) {
            StorageCache.Clear();
            StoreToDiskFileNames.Clear();
        }
        
        private static IObservable<Unit> DailyBackup(this XafApplication application)
            => application.WhenLoggedOn()
                .Select(_ => application.Model.ToReactiveModule<IModelReactiveModulesStoreToDisk>().StoreToDisk)
                .Where(storeToDisk => storeToDisk.DailyBackup&&Directory.Exists(storeToDisk.Folder)).ObserveOnDefault()
                .SelectMany(disk => {
                    if (!Directory.Exists(disk.Folder)) return Observable.Empty<string>();
                    var jsonFles = Directory.GetFiles(disk.Folder, "*.json");
                    var directoryName = $"{disk.Folder}\\{DateTime.Now:yyyy.MM.dd}";
                    if (!jsonFles.Any() || Directory.Exists(directoryName)) return Observable.Empty<string>();
                    Directory.CreateDirectory(directoryName);
                    return jsonFles.Execute(file => File.Copy(file, Path.Combine(directoryName,Path.GetFileName(file)))).ToNowObservable();
                })
                .ToUnit();

        private static IObservable<(object[] objects, Dictionary<object,JsonNode> JsonArray)> LoadFromDisk(this (IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details) source,(IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath, StoreToDiskAttribute attribute) data) 
            => Observable.Defer(() => data.attribute.DeserializeStorage(data)
                .SelectMany(jsonNodes => {
                    var newObjects = source.details.Where(details => details.modification == ObjectModification.New).Select(t => t.instance).ToArray();
                    return newObjects.Length == 0 ? (newObjects, jsonNodes).Observe() : source.details.LoadFromDisk(data.keyMember, data.memberInfos, newObjects, jsonNodes, source.objectSpace);
                }));

        private static IObservable<(object[] objects, Dictionary<object,JsonNode> JsonArray)> LoadFromDisk(this (object instance, ObjectModification modification)[] source, IMemberInfo keyMember,
            IMemberInfo[] memberInfos, object[] newObjects, Dictionary<object, JsonNode> jsonArray, IObjectSpace space) 
            => newObjects.ToNowObservable()
                .SelectMany(newObject => {
                    var key = (string)keyMember.GetValue(newObject);
                    if (key==null||!jsonArray.TryGetValue(key,out var jToken))return Observable.Empty<object>();
                    return memberInfos.ToNowObservable()
                        .WhenDefault(info => info.GetValue(newObject))
                        .Do(info => {
                            var value = ((JsonValue)jToken[info.Name])?.Deserialize(
                                !info.MemberTypeInfo.IsPersistent
                                    ? info.MemberType
                                    : info.MemberTypeInfo.KeyMember.MemberType);
                            var change = !info.MemberTypeInfo.IsPersistent ? value :
                                value == null ? null : space.GetObjectByKey(info.MemberType, value);
                            info.SetValue(newObject, change);
                        })
                        .Select(_ => newObject);
                })
                .BufferUntilCompleted()
                .Select(_ => (source.Where(t => t.modification!=ObjectModification.New)
                    .Select(t => space.GetObject(t.instance)).Concat(newObjects).ToArray(), JsonArray: jsonArray));


        private static IObservable<Dictionary<object, JsonNode>> DeserializeStorage(this StoreToDiskAttribute attribute,
            (IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath, StoreToDiskAttribute
                attribute) data) {
            var key = Path.GetFileNameWithoutExtension(data.filePath);
            return StorageCache.TryGetValue(key, out var value) ? value.Observe()
                : new FileInfo(data.filePath).WhenFileReadAsBytes()
                    .Select(bytes => {
                        var dictionary = new Dictionary<object, JsonNode>();
                        if (bytes.Length == 0) {
                            dictionary.Add(attribute.Key,new JsonArray());
                        }
                        else {
                            attribute.Protection.UnProtect(bytes).DeserializeJsonNode().ToJsonArray()
                                .Execute(node => {
                                    var jsonNode = node[attribute.Key].Deserialize(data.keyMember.MemberType)!;
                                    dictionary.Add(jsonNode, node);
                                })
                                .Enumerate();    
                        }
                        
                        return dictionary;
                        
                    })
                    .Where(result => StorageCache.TryAdd(key, result));
        }


        public static IObservable<Unit> StoreToDisk(this XafApplication application, string directory)
            => application.StoreToDiskData(directory)
                .SelectMany(data => application.WhenProviderCommittingDetailed(data.typeInfo.Type,ObjectModification.NewOrUpdated,true,[]).Where(details => details.details.Length>0)
                    .SelectMany(committed => {
                        var modifiedObjects = committed.objectSpace.ModifiedObjects(ObjectModification.NewOrUpdated).Select(t => t.instance).ToArray();
                        return committed.LoadFromDisk(data).Zip(committed.objectSpace.WhenCommitted().Take(1)).ToFirst()
                            .TakeUntil(committed.objectSpace.WhenDisposed().MergeToUnit(committed.objectSpace.WhenRollingBack()))
                            .StoreToDisk(modifiedObjects, data);
                    }))
                
                .ToUnit();
        
        private static IObservable<JsonObject[]> StoreToDisk(this IObservable<(object[] objects, Dictionary<object,JsonNode> JsonArray)> source,
                object[] committed, (IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath, StoreToDiskAttribute attribute) data)
            => source.SelectMany(loadFromDisk => data.ObjectsToStore(committed,  loadFromDisk.objects.WhereNotDefault().ToArray())
                .StoreToDisk(loadFromDisk.JsonArray, data.keyMember, data.memberInfos, data.filePath, data.attribute));

        private static object[] ObjectsToStore(this (IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath, StoreToDiskAttribute attribute) data,
            object[] committed, object [] loadedObjects) {
            var loadedKeys = loadedObjects.Select(o => data.keyMember.GetValue(o)).ToHashSet();
            var valueTuples = committed.Where(details => !loadedKeys.Contains(data.keyMember.GetValue(details))).ToArray();
            return valueTuples.Select(updated => updated).Concat(loadedObjects).ToArray();
        }

        private static IObservable<(IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath, StoreToDiskAttribute attribute)> StoreToDiskData(
                this XafApplication application, string directory)
            => application.TypesInfo.PersistentTypes
                .Select(info => info).Attributed<StoreToDiskAttribute>().ToNowObservable()
                .Select(t => (keyMember:t.typeInfo.FindMember(t.attribute.Key),memberInfos:t.attribute.Properties.Select(property =>t.typeInfo.FindMember(property)).ToArray(),t.attribute,t.typeInfo))
                .SelectMany(t => new DirectoryInfo(directory).WhenDirectory().Select(_ => t.typeInfo.EnsureFile(directory))
                    .Select(filePath => ( t.keyMember, t.memberInfos, t.typeInfo, filePath,t.attribute)));
        
        private static IObservable<JsonObject[]> StoreToDisk(this object[] objects, Dictionary<object, JsonNode> jsonArray,
            IMemberInfo keyMember, IMemberInfo[] memberInfos, string filePath, StoreToDiskAttribute attribute) 
            => objects.ToNowObservable().SelectMany(instance => {
                    var jtoken = memberInfos.GetObject(keyMember,   instance);
                    return jtoken != null ? memberInfos.ToNowObservable()
                            .Do(memberInfo => {
                                var memberValue = memberInfo.GetMemberValue(instance);
                                if (memberValue.IsDefaultValue()) return;
                                jtoken[memberInfo.Name] = memberValue.ToJsonNode();
                            })
                            .ConcatIgnoredValue(jtoken)
                        : Observable.Empty<JsonObject>();
                }).BufferUntilCompleted(true)
                .Do(jsonObjects => {
                    var removeExisting = objects.RemoveExisting(jsonArray, keyMember);
                    var array = new JsonArray(jsonObjects.Concat(removeExisting.Keys.Select(key => removeExisting[key])
                        .Select(node => node is JsonArray { Count: 0 } ? null : node.Deserialize<JsonObject>())).WhereNotDefault().Cast<JsonNode>().ToArray());
                    array.SaveFile(attribute.Protection, filePath,removeExisting);
                });

        private static Dictionary<object, JsonNode> RemoveExisting(this object[] objects,Dictionary<object, JsonNode> jsonArray  ,IMemberInfo keyMember) {
            objects.ForEach(o => {
                var value = keyMember.GetValue(o);
                if (value==null)return;
                jsonArray.Remove(value);
            });
            return jsonArray;
        }

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        private static void SaveFile(this JsonArray json,DataProtectionScope? scope,string filePath,Dictionary<object,JsonNode> dictionary) {
            StorageCache[Path.GetFileNameWithoutExtension(filePath)] = dictionary;
            var bytes = json.Utf8Bytes(new JsonSerializerOptions(){WriteIndented = true});
            (scope != null ? bytes.Protect(scope.Value) : bytes).Save(filePath);
        }

        private static JsonObject GetObject(this IMemberInfo[] memberInfos,IMemberInfo keyMember,   object instance) {
            var memberValue = keyMember.GetMemberValue(instance);
            var properties = memberInfos.Select(info => (info.Name, memberValue:info.GetMemberValue(instance)))
                .AddItem((keyMember.Name, memberValue)).Where(t => !t.memberValue.IsDefaultValue()).ToArray();
            return properties.Length > 1 ? properties.ToDictionary().SerializeToNode()!.AsObject() : null;
        }
        
        private static string EnsureFile(this  ITypeInfo typeInfo,string directory){
            var filePath = $"{new DirectoryInfo(directory).FullName}\\{typeInfo.Type.StoreToDiskFileName()}";
            if (!File.Exists(filePath)){
                File.CreateText(filePath);
            }
            return filePath;
        }

        
        public static string StoreToDiskFileName(this Type type) 
            => StoreToDiskFileNames.GetOrAdd(type, type1 => type1.FullName.CleanCodeName().JoinString(".json"));

        

    }
}