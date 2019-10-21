using System;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.DB.Helpers;

namespace TestApplication.EasyTest
{
    public class InMemoryDataStoreProvider : DevExpress.Xpo.DB.InMemoryDataStore
    {
        new public const string XpoProviderTypeString = "InMemoryDataSet";

        static InMemoryDataStoreProvider() => Register();

        new public static void Register()
            => RegisterDataStoreProvider(XpoProviderTypeString, new DataStoreCreationFromStringDelegate(CreateProviderFromString));

        private static object syncRoot = new object();

        private static InMemoryDataStore _savedDataSet;
        private static InMemoryDataStore savedDataSet
        {
            get => _savedDataSet;
            set
            {
                lock (syncRoot)
                {
                    _savedDataSet = value;
                }
            }
        }

        private static InMemoryDataStore _store;
        private static InMemoryDataStore store
        {
            get => _store;
            set
            {
                lock (syncRoot)
                {
                    _store = value;
                }
            }
        }

        new public static IDataStore CreateProviderFromString(string connectionString, AutoCreateOption autoCreateOption, out IDisposable[] objectsToDisposeOnDisconnect)
        {
            if (store == null)
            {
                store = new InMemoryDataStore(AutoCreateOption.DatabaseAndSchema);
            }

            objectsToDisposeOnDisconnect = new IDisposable[] { };

            return store;
        }

        public static bool HasData { get { return savedDataSet != null; } }

        public static void Save()
        {
            if (!HasData && store != null)
            {
                savedDataSet = store;
            }
        }

        public static void Reload()
        {
            if (HasData && store != null)
            {
                store.ReadFromInMemoryDataStore(savedDataSet);
            }
        }
    }
}
