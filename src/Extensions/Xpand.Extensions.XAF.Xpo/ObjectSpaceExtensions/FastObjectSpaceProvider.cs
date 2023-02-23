using System;
using System.Data;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.DC.Xpo;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using DevExpress.Xpo.Metadata.Helpers;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions{
    public class FastObjectSpaceProvider:XPObjectSpaceProvider{
        public FastObjectSpaceProvider(IXpoDataStoreProvider dataStoreProvider, bool threadSafe, bool useSeparateDataLayers = false) : base(dataStoreProvider, threadSafe, useSeparateDataLayers){
        }

        public FastObjectSpaceProvider(string connectionString, IDbConnection connection, bool threadSafe, bool useSeparateDataLayers = false) : base(connectionString, connection, threadSafe, useSeparateDataLayers){
        }

        public FastObjectSpaceProvider(IXpoDataStoreProvider dataStoreProvider, ITypesInfo typesInfo, XpoTypeInfoSource xpoTypeInfoSource, bool threadSafe, bool useSeparateDataLayers = false) : base(dataStoreProvider, typesInfo, xpoTypeInfoSource, threadSafe, useSeparateDataLayers){
        }

        public FastObjectSpaceProvider(IXpoDataStoreProvider dataStoreProvider) : base(dataStoreProvider){
        }

        public FastObjectSpaceProvider(string connectionString, IDbConnection connection) : base(connectionString, connection){
        }

        public FastObjectSpaceProvider(IDbConnection connection) : base(connection){
        }

        public FastObjectSpaceProvider(string connectionString) : base(connectionString){
        }

        public FastObjectSpaceProvider(IXpoDataStoreProvider dataStoreProvider, ITypesInfo typesInfo, XpoTypeInfoSource xpoTypeInfoSource) : base(dataStoreProvider, typesInfo, xpoTypeInfoSource){
        }

        protected override UnitOfWork CreateUnitOfWork(IDataLayer dataLayer) => new FastUnitOfWork(dataLayer);
    }
    
    public class FastUnitOfWork : UnitOfWork {
        public FastUnitOfWork() {
        }

        public FastUnitOfWork(XPDictionary dictionary) : base(dictionary) 
            => TrackPropertiesModifications = true;

        public FastUnitOfWork(IDataLayer layer)
            : this(layer,Array.Empty<IDisposable>()){
        }
        public FastUnitOfWork(IDataLayer layer, IDisposable[] disposeOnDisconnect) : base(layer, disposeOnDisconnect) 
            => TrackPropertiesModifications = true;

        public FastUnitOfWork(IObjectLayer layer)
            : this(layer, Array.Empty<IDisposable>()) {
        }
        
        public FastUnitOfWork(IObjectLayer layer, params IDisposable[] disposeOnDisconnect)
            : base(layer, disposeOnDisconnect) =>
            TrackPropertiesModifications = true;

        protected override MemberInfoCollection GetPropertiesListForUpdateInsert(object theObject, bool isUpdate, bool addDelayedReference){
            var defaultMembers = base.GetPropertiesListForUpdateInsert(theObject, isUpdate, addDelayedReference);
            if (TrackPropertiesModifications && isUpdate){
                var members = new MemberInfoCollection(GetClassInfo(theObject));
                foreach (var mi in base.GetPropertiesListForUpdateInsert(theObject, true, addDelayedReference))
                    if (mi is ServiceField || mi.GetModified(theObject))
                        members.Add(mi);
                return members;
            }
        
            return defaultMembers;
        }

    }

}