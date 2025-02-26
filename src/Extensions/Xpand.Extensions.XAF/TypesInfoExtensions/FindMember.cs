using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.ExpressionExtensions;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.TypesInfoExtensions{
	public static partial class TypesInfoExtensions {
		public static IEnumerable<IMemberInfo> Members<TAttribute>(this IEnumerable<(TAttribute attribute, IMemberInfo memberInfo)> source) 
			=> source.Select(t => t.memberInfo);
		
		public static IEnumerable<(IMemberInfo info,object value)> MembersValue(this IEnumerable<IMemberInfo> source,object instance)
			=> source.Select(memberInfo => (memberInfo,value:memberInfo.GetValue(instance)));
		
		public static IEnumerable<(TAttribute attribute, IMemberInfo memberInfo)> AttributedMembers<TAttribute>(this IEnumerable<ITypeInfo> source)   
			=> source.SelectMany(info => info.AttributedMembers<TAttribute>());

		public static IEnumerable<ITypeInfo> Types<TAttribute>(this IEnumerable<(TAttribute attribute, ITypeInfo typeInfo)> source,bool includeDerivedTypes=false) 
			=> source.SelectMany(t => includeDerivedTypes?t.typeInfo.Descendants.StartWith(t.typeInfo).Distinct():t.typeInfo.YieldItem());
		
		public static IEnumerable<(TAttribute attribute, ITypeInfo typeInfo)> Attributed<TAttribute>(this IEnumerable<ITypeInfo> source)   
			=> source.SelectMany(info => info.Attributed<TAttribute>());
		
		public static IEnumerable<(TAttribute attribute, ITypeInfo typeInfo)> Attributed<TAttribute>(this IEnumerable<Type> source)   
			=> source.Select(type => type.ToTypeInfo()).Attributed<TAttribute>();
		
		public static IEnumerable<IMemberInfo> Members<TAttribute>(this IEnumerable<ITypeInfo> source)   
			=> source.SelectMany(info => info.AttributedMembers<TAttribute>()).Members();

		public static IEnumerable<(TAttribute attribute,IMemberInfo memberInfo)> AttributedMembers<TAttribute>(this ITypeInfo info,Func<TAttribute,bool> where=null)  
			=> info.Members.SelectMany(memberInfo => memberInfo.FindAttributes<Attribute>().OfType<TAttribute>()
				.Where(arg => where?.Invoke(arg)??true).Select(attribute => (attribute, memberInfo)));
		
		public static IEnumerable<(TAttribute attribute,ITypeInfo typeInfo)> Attributed<TAttribute>(this ITypeInfo info,bool includeBaseTypes=false) {
			var infos = info.YieldItem();
			if (includeBaseTypes) {
				infos = info.FromHierarchy(typeInfo => typeInfo.Base).Prepend(info);
			}
			return infos.Distinct().SelectMany(typeInfo => typeInfo.FindAttributes<Attribute>(includeBaseTypes).OfType<TAttribute>().Select(attribute => (attribute, info)));
		}

		public static IMemberInfo FindMember<T>(this ITypeInfo typeInfo,Expression<Func<T, object>> memberName) 
            => typeInfo.FindMember(memberName.MemberExpressionName());
	}
}