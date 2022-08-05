using System;
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
        
        static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenProviderCommittingDetailed1(
            this XafApplication application,Type objectType,ObjectModification objectModification,Func<object,bool> criteria=null,bool emitUpdatingObjectSpace=false,params string[] modifiedProperties)
            => application.WhenProviderObjectSpaceCreated(emitUpdatingObjectSpace).WhenCommittingDetailed1(objectType,objectModification, criteria,modifiedProperties);
        static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenCommittingDetailed1(this IObservable<IObjectSpace> source,
            Type objectType,ObjectModification objectModification,Func<object,bool> criteria,params string[] modifiedProperties)
            => source.SelectMany(objectSpace => 
                objectSpace.WhenCommitingDetailed1(objectType,objectModification,false,criteria,modifiedProperties).TakeUntil(objectSpace.WhenDisposed()));
        
        static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)>
            WhenCommitingDetailed1(this IObjectSpace objectSpace,Type objectType, ObjectModification objectModification, bool emitAfterCommit,Func<object,bool> criteria=null,
                params string[] modifiedProperties) 
            => objectSpace.WhenCommitingDetailed1(objectType, emitAfterCommit, objectModification,criteria,modifiedProperties);

        static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenCommitingDetailed1(
            this IObjectSpace objectSpace,Type objectType, bool emitAfterCommit, ObjectModification objectModification,Func<object,bool> criteria=null, params string[] modifiedProperties) 
            => objectSpace.WhenModifiedObjects1(objectType,modifiedProperties)
                .Select(_ => objectSpace.WhenCommiting()
                    .SelectMany(_ => {
                        var modifiedObjects = objectSpace.ModifiedObjects(objectType, objectModification)
                            .Where(t => criteria==null|| criteria.Invoke(t.instance)).ToArray();
                        return modifiedObjects.Any() ? emitAfterCommit ? objectSpace.WhenCommitted().FirstAsync().Select(space => (space, modifiedObjects))
                            : (objectSpace, modifiedObjects).ReturnObservable() : Observable.Empty<(IObjectSpace, (object instance, ObjectModification modification)[])>();
                    })).Switch();
        private static bool PropertiesMatch(this string[] properties, (IObjectSpace objectSpace, ObjectChangedEventArgs e) t) 
            => !properties.Any()||(t.e.MemberInfo != null && properties.Contains(t.e.MemberInfo.Name) ||
                                   t.e.PropertyName != null && properties.Contains(t.e.PropertyName));

        static IObservable<object> WhenModifiedObjects1(this IObjectSpace objectSpace,Type objectType, params string[] properties) 
            => Observable.Defer(() => objectSpace.WhenObjectChanged().Where(t => objectType.IsInstanceOfType(t.e.Object) && properties.PropertiesMatch( t))
                    .Select(_ => _.e.Object).Take(1))
                .RepeatWhen(observable => observable.SelectMany(_ => objectSpace.WhenModifyChanged().Where(space => !space.IsModified).Take(1)))
                .TakeUntil(objectSpace.WhenDisposed());


        
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
                .SelectMany(t => application.WhenProviderCommittedDetailed(t.typeInfo.Type,ObjectModification.NewOrUpdated,emitUpdatingObjectSpace:true)
                    .SelectMany(t1 => t1.details.Where(t2 =>t2.modification==ObjectModification.New ).Select(t3 => t3.instance).ToArray()
                        .LoadFromDisk(application, t.keyMember, t.memberInfos, t.typeInfo, t.filePath, t.attribute)
                        .SelectMany(t2 => (t2.objects.Concat(t1.details
                            .Where(t3 => t3.modification == ObjectModification.Updated)
                            .Select(t4 => t4.instance).ToArray()).ToArray(),t2.jArray).StoreToDisk(t.keyMember, t.memberInfos, t.filePath, t.attribute))))
                .ToUnit();
        
        private static IObservable<(IMemberInfo keyMember, IMemberInfo[] memberInfos, ITypeInfo typeInfo, string filePath, StoreToDiskAttribute attribute)> StoreToDiskData(
                this XafApplication application, string directory)
            => application.TypesInfo.PersistentTypes.Attributed<StoreToDiskAttribute>().ToNowObservable()
                .Select(t => (keyMember:t.typeInfo.FindMember(t.attribute.Key),memberInfos:t.attribute.Properties.Select(property =>t.typeInfo.FindMember(property)).ToArray(),t.attribute,t.typeInfo))
                .SelectMany(t => new DirectoryInfo(directory).WhenDirectory().Select(_ => t.typeInfo.EnsureFile(directory))
                    .Select(filePath => ( t.keyMember, t.memberInfos, t.typeInfo, filePath,t.attribute)));
        
        
        private static IObservable<JObject[]> StoreToDisk(this (object[] objects, JArray jArray) source,IMemberInfo keyMember, IMemberInfo[] memberInfos, string filePath, StoreToDiskAttribute attribute) 
            => source.objects.ToNowObservable().SelectMany(instance => {
                var jtoken = source.jArray.GetToken(keyMember, memberInfos,  instance);
                return memberInfos.ToNowObservable().Do(memberInfo =>
                        jtoken[memberInfo.Name] = memberInfo.GetValue(instance).ToJToken())
                    .ConcatIgnoredValue(jtoken);
            }).BufferUntilCompleted(true)
                .Do(objects => attribute.SaveFile(filePath,  new JArray(objects.Concat(source.ReplaceExisting(keyMember))).ToString()))
                .TraceStoreToDisk();

        private static JArray ReplaceExisting(this (object[] objects, JArray jArray) source ,IMemberInfo keyMember) {
            source.objects.ForEach(o => source.jArray.Where(token => keyMember.Match(token, o)).Take(1).ToArray()
                    .ForEach(token => source.jArray.Remove(token)));
            return source.jArray;
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