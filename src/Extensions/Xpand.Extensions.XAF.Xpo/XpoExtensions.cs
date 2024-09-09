using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.DC.Xpo;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.Helpers;
using DevExpress.Xpo.Metadata;
using DevExpress.Xpo.Providers;
using Fasterflect;
using Swordfish.NET.Collections.Auxiliary;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.Xpo.Attributes;
using Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions;

namespace Xpand.Extensions.XAF.Xpo {
    public interface IXpoAttributeValue {
        string Value { get; }
    }

    
    public static class XpoExtensions {
        public static IDataLayer GetDataLayer(this ITypesInfo typesInfo,string connectionString,AutoCreateOption autoCreateOption=AutoCreateOption.None) 
            => XpoDefault.GetDataLayer(connectionString, ((TypesInfo)typesInfo).EntityStores.OfType<XpoTypeInfoSource>().First().XPDictionary, autoCreateOption);
        
        internal static void CustomizeTypesInfo(ITypesInfo typesInfo) {
            CreateXpAttributeValueAttributes();
            RuntimeAssociationAttributes(typesInfo);
        }

        private static void RuntimeAssociationAttributes(this ITypesInfo typesInfo) 
            => typesInfo.PersistentTypes.AttributedMembers<RuntimeAssociationAttribute>().ToArray()
                .Select(t => (t.attribute, memberInfo: t.memberInfo.Owner.QueryXPClassInfo().FindMember(t.memberInfo.Name)))
                .ForEach(t => {
                    var providedAssociationAttribute = (RuntimeAssociationAttribute)t.memberInfo.FindAttributeInfo(typeof(RuntimeAssociationAttribute));
                    var customMemberInfo = typesInfo.CreateMemberInfo(t.memberInfo, providedAssociationAttribute, t.memberInfo.GetAssociationAttribute(providedAssociationAttribute));
                    t.memberInfo.AddExtraAttributes(providedAssociationAttribute, customMemberInfo);
                });

        private static void CreateXpAttributeValueAttributes(){
            new[]{ nameof(SingleObjectAttribute), nameof(PropertyConcatAttribute) }.ToArray().ForEach(attributeName => {
                var lastObjectAttributeType =
                    AppDomain.CurrentDomain.GetAssemblyType($"Xpand.Extensions.XAF.Xpo.Attributes.{attributeName}");
                lastObjectAttributeType?.Method("Configure", Flags.StaticAnyVisibility).Call(null);
            });
        }

        public static XPMemberInfo CreateCollection(this ITypesInfo typeInfo, Type typeToCreateOn,
            Type typeOfCollection, string associationName) 
            => typeInfo.CreateCollection( typeToCreateOn, typeOfCollection, associationName, true);
        
        static XPMemberInfo CreateCollection(this ITypesInfo typeInfo, Type typeToCreateOn, Type typeOfCollection, string associationName,  bool refreshTypesInfo,
                                                          string propertyName, bool isManyToMany) 
            => typeInfo.CreateCollection( typeToCreateOn, typeOfCollection, associationName, propertyName, refreshTypesInfo, isManyToMany);

        public static XPMemberInfo CreateCollection(this ITypesInfo typeInfo, Type typeToCreateOn, Type typeOfCollection, string associationName,  bool refreshTypesInfo,
                                                          string propertyName) 
            => typeInfo.CreateCollection( typeToCreateOn, typeOfCollection, associationName,  propertyName, refreshTypesInfo, false);

        public static XPMemberInfo CreateCollection(this ITypesInfo typeInfo, Type typeToCreateOn, Type typeOfCollection, string associationName,  bool refreshTypesInfo) 
            => CreateCollection(typeInfo, typeToCreateOn, typeOfCollection, associationName,  refreshTypesInfo, typeOfCollection.Name + "s");

        public static XPMemberInfo CreateCollection(this ITypesInfo typeInfo, Type typeToCreateOn, Type typeOfCollection, string associationName,  string collectionName) 
            => typeInfo.CreateCollection( typeToCreateOn, typeOfCollection, associationName,  collectionName, true);

        static XPMemberInfo CreateCollection(this ITypesInfo typeInfo, Type typeToCreateOn, Type typeOfCollection, string associationName,  string collectionName, bool refreshTypesInfo,
                                                          bool isManyToMany) {
            XPMemberInfo member = null;
            if (TypeIsRegister(typeInfo, typeToCreateOn)) {
                XPClassInfo xpClassInfo = typeInfo.FindTypeInfo(typeToCreateOn).QueryXPClassInfo();
                member = xpClassInfo.FindMember(collectionName) ??
                         xpClassInfo.CreateMember(collectionName, typeof(XPCollection), true);
                if (member.FindAttributeInfo(typeof(AssociationAttribute))==null)
                    member.AddAttribute(new AssociationAttribute(associationName, typeOfCollection){
                        UseAssociationNameAsIntermediateTableName = isManyToMany
                    });
                if (refreshTypesInfo)
                    typeInfo.RefreshInfo(typeToCreateOn);
            }
            return member;

        }

        public static XPMemberInfo CreateCollection(this ITypesInfo typeInfo, Type typeToCreateOn,
            Type typeOfCollection, string associationName, string collectionName, bool refreshTypesInfo) 
            => typeInfo.CreateCollection(typeToCreateOn, typeOfCollection, associationName, collectionName, refreshTypesInfo, false);

        public static List<XPMemberInfo> CreateBothPartMembers(this ITypesInfo typesInfo, Type typeToCreateOn, Type otherPartType) 
            => typesInfo.CreateBothPartMembers( typeToCreateOn, otherPartType,  false);

        public static List<XPMemberInfo> CreateBothPartMembers(this ITypesInfo typesInfo, Type typeToCreateOn, Type otherPartMember,  bool isManyToMany) 
            => typesInfo.CreateBothPartMembers( typeToCreateOn, otherPartMember, isManyToMany, Guid.NewGuid().ToString());

        public static List<XPMemberInfo> CreateBothPartMembers(this ITypesInfo typesInfo, Type typeToCreateOn, Type otherPartMember, bool isManyToMany, string association,
                                                                     string createOnPropertyName, string otherPartPropertyName) {
            var infos = new List<XPMemberInfo>();
            var member = isManyToMany ? CreateCollection(typesInfo, typeToCreateOn, otherPartMember, association, false, createOnPropertyName, true)
                                            : CreateMember(typesInfo, typeToCreateOn, otherPartMember, association,  createOnPropertyName, false);

            if (member != null) {
                infos.Add(member);
                member = isManyToMany ? typesInfo.CreateCollection( otherPartMember, typeToCreateOn, association, false, otherPartPropertyName, true)
                             : typesInfo.CreateCollection( typeToCreateOn, otherPartMember, association, false, otherPartPropertyName);

                if (member != null)
                    infos.Add(member);
            }

            typesInfo.RefreshInfo(typeToCreateOn);
            typesInfo.RefreshInfo(otherPartMember);
            return infos;

        }

        public static XPMemberInfo CreateMember(this ITypesInfo typesInfo, Type typeToCreateOn, Type typeOfMember, string associationName) 
            => typesInfo.CreateMember( typeToCreateOn, typeOfMember, associationName,  true);

        public static XPMemberInfo CreateMember(this ITypesInfo typesInfo, Type typeToCreateOn, Type typeOfMember, string associationName,  bool refreshTypesInfo)
            => typesInfo.CreateMember( typeToCreateOn, typeOfMember, associationName,  typeOfMember.Name, refreshTypesInfo);

        public static XPMemberInfo CreateMember(this ITypesInfo typesInfo, Type typeToCreateOn, Type typeOfMember, string associationName,  string propertyName) 
            => typesInfo.CreateMember( typeToCreateOn, typeOfMember, associationName,  propertyName, true);

        private static bool TypeIsRegister(ITypesInfo typeInfo, Type typeToCreateOn) 
            => XafTypesInfo.Instance.FindTypeInfo(typeToCreateOn).IsDomainComponent ||
               typeInfo.PersistentTypes.FirstOrDefault(info => info.Type == typeToCreateOn) != null;

        public static XPMemberInfo CreateMember(this ITypesInfo typesInfo, Type typeToCreateOn, Type typeOfMember, string associationName,  string propertyName, bool refreshTypesInfo) {
            XPMemberInfo member = null;
            if (TypeIsRegister(typesInfo, typeToCreateOn)) {
                XPClassInfo xpClassInfo = typesInfo.FindTypeInfo(typeToCreateOn).QueryXPClassInfo();
                member = xpClassInfo.FindMember(propertyName);
                if (member == null) {
                    member = xpClassInfo.CreateMember(propertyName, typeOfMember,
                        new AssociationAttribute(associationName, typeOfMember));
                    if (refreshTypesInfo)
                        typesInfo.RefreshInfo(typeToCreateOn);
                }
            }
            return member;
        }

        public static List<XPMemberInfo> CreateBothPartMembers(this ITypesInfo typesInfo, Type typeToCreateOn, Type otherPartMember,  bool isManyToMany, string association) {

            var infos = new List<XPMemberInfo>();
            var member = isManyToMany ? CreateCollection(typesInfo, typeToCreateOn, otherPartMember, association,  false)
                                            : CreateMember(typesInfo, otherPartMember, typeToCreateOn, association,  false);

            if (member != null) {
                infos.Add(member);
                member = isManyToMany ? CreateCollection(typesInfo, otherPartMember, typeToCreateOn, association, false)
                             : CreateCollection(typesInfo, typeToCreateOn, otherPartMember, association, false);

                if (member != null)
                    infos.Add(member);
            }

            typesInfo.RefreshInfo(typeToCreateOn);
            typesInfo.RefreshInfo(otherPartMember);

            return infos;
        }

        
        public static XPClassInfo FindDCXPClassInfo(this ITypeInfo typeInfo) {
            var xpoTypeInfoSource = ((XpoTypeInfoSource) ((TypeInfo) typeInfo).Source);
            if (DesignerOnlyCalculator.IsRunTime) {
                var generatedEntityType = xpoTypeInfoSource.GetGeneratedEntityType(typeInfo.Type);
                return generatedEntityType == null ? null : xpoTypeInfoSource.XPDictionary.GetClassInfo(generatedEntityType);
            }
            var className = typeInfo.Name + "BaseDCDesignTimeClass";
            var xpClassInfo = xpoTypeInfoSource.XPDictionary.QueryClassInfo("", className);
            return xpClassInfo ?? new XPDataObjectClassInfo(xpoTypeInfoSource.XPDictionary, className);
        }
        
        public static XPClassInfo QueryXPClassInfo(this ITypeInfo typeInfo){
            var typeInfoSource = ((TypeInfo)typeInfo).Source as XpoTypeInfoSource;
            return typeInfoSource?.XPDictionary.QueryClassInfo(typeInfo.Type);
        }
        
        internal static IMemberInfo[] Configure<T>() where T : Attribute,IXpoAttributeValue 
            => XafTypesInfo.Instance.PersistentTypes.SelectMany(info => info.Members)
                .Select(info => {
                    var attribute = info.FindAttribute<T>();
                    if (attribute != null) {
                        info.AddAttribute(new PersistentAliasAttribute(attribute.Value));
                    }
                    return info;
                }).ToArray();
        public static void FireChanged(this IXPReceiveOnChangedFromArbitrarySource source, string propertyName) 
            => source.FireChanged(propertyName);

        public static void SetCriteria<T>(this XPBaseCollection collection, Expression<Func<T, bool>> lambda) 
            => collection.Criteria = CriteriaOperator.FromLambda(lambda);
        
        public static void SetFilter<T>(this XPBaseCollection collection, Expression<Func<T, bool>> lambda) 
            => collection.Filter = CriteriaOperator.FromLambda(lambda);

        public static IDbConnection Connection(this UnitOfWork unitOfWork){
            var dataLayer = unitOfWork.DataLayer;
            var connectionProvider = ((BaseDataLayer)dataLayer).ConnectionProvider;
            if (connectionProvider is DataCacheNode){
                return (IDbConnection)connectionProvider.GetPropertyValue("Nested")
                    .GetFieldValue("Connection");
            }

            if (connectionProvider is DataStorePool pool){
                var connectionProviderSql = (ConnectionProviderSql)pool.AcquireReadProvider();
                var dbConnection = connectionProviderSql.Connection;
                pool.ReleaseReadProvider(connectionProviderSql);
                return dbConnection;
            }

            return connectionProvider is not STASafeDataStore ? connectionProvider is ConnectionProviderSql provider ? provider.Connection : null
                : connectionProvider.GetFieldValue("DataStore").Cast<ConnectionProviderSql>().Connection;
        }
        
        public static SqlConnection NewSQLConnection(this XafApplication application,Type objectType=null) {
            objectType ??= application.TypesInfo.PersistentTypes.First(info => info.IsPersistent).Type;
            using var objectSpace = application.CreateObjectSpace(objectType);
            var dbConnection = objectSpace.Connection();
            if (dbConnection!=null) {
                var sqlConnection = new SqlConnection(dbConnection.ConnectionString);
                sqlConnection.Open();
                return sqlConnection;
            }
            return null;
        }

        public static string XpoMigrateDatabaseScript(this XafApplication application, IDataStore dataStore=null)
            => dataStore is not IUpdateSchemaSqlFormatter sqlFormatter || !((ConnectionProviderSql)dataStore).Connection.DbExists() ? null
                : sqlFormatter.FormatUpdateSchemaScript(((IDataStoreSchemaMigrationProvider)dataStore)
                    .CompareSchema(new ReflectionDictionary().GetDataStoreSchema(application.TypesInfo.PersistentTypes
                            .Where(info => info.IsPersistent).Select(info => info.Type).ToArray()),
                        new SchemaMigrationOptions()));

        public static void XpoMigrateDatabase(this XafApplication application, string connectionString=null) {
            var provider = XpoDefault.GetConnectionProvider(connectionString??application.ConnectionString, AutoCreateOption.DatabaseAndSchema);
            var sql = application.XpoMigrateDatabaseScript(provider);
            if (sql.IsNullOrEmpty()) return;
            var command = ((ConnectionProviderSql)provider).Connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        
    }
}