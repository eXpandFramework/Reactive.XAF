using System;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo.DB;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.XAF.Xpo.ConnectionProviders {
    public class CachedDataStoreProvider : ConnectionStringDataStoreProvider, IXpoDataStoreProvider {
        static CachedDataStoreProvider() {
            Factory = () => default;
            CreateStore = () => default;
            CustomCreateUpdatingStore = () => default;
        }

        private static readonly Lazy<CachedDataStoreProvider> Lazy = new(() 
            => Factory() ?? new CachedDataStoreProvider(StaticConnectionString));

        public static string StaticConnectionString { get; set; }

        public static Func<CachedDataStoreProvider> Factory { get; set; }

        private static IDisposable[] _rootDisposableObjects;
        private static DataCacheRoot _root;

        public static CachedDataStoreProvider Instance => Lazy.Value;

        public CachedDataStoreProvider(string connectionString) : base(connectionString) {
        }

        public static readonly Func<(bool allowUpdateSchema, IDisposable[] rootDisposables, IDataStore dataStore)> CustomCreateUpdatingStore;

        public static readonly Func<(IDisposable[] rootDisposables, IDataStore dataStore)> CreateStore;

        public new IDataStore CreateWorkingStore(out IDisposable[] disposableObjects) 
            => ((IXpoDataStoreProvider) this).CreateWorkingStore(out disposableObjects);

        IDataStore IXpoDataStoreProvider.CreateUpdatingStore(bool allowUpdateSchema,
            out IDisposable[] disposableObjects) {
            var store = CustomCreateUpdatingStore();
            if (store.IsDefaultValue()) {
                return base.CreateUpdatingStore(allowUpdateSchema, out disposableObjects);
            }

            disposableObjects = store.rootDisposables;
            return store.dataStore;
        }

        IDataStore IXpoDataStoreProvider.CreateWorkingStore(out IDisposable[] disposableObjects) {
            if (_root == null) {
                var tuple = CreateStore();
                IDataStore baseDataStore;
                if (tuple.IsDefaultValue()) {
                    baseDataStore = base.CreateWorkingStore(out _rootDisposableObjects);
                }
                else {
                    baseDataStore = tuple.dataStore;
                    _rootDisposableObjects = tuple.rootDisposables;
                }

                _root = new DataCacheRoot(baseDataStore);
            }

            disposableObjects = Array.Empty<IDisposable>();
            return new DataCacheNode(_root);
        }

        public static void ResetDataCacheRoot() {
            _root = null;
            if (_rootDisposableObjects != null) {
                foreach (var disposableObject in _rootDisposableObjects) disposableObject.Dispose();
                _rootDisposableObjects = null;
            }
        }
    }
}