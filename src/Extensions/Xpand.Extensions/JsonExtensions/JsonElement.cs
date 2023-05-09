using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.JsonExtensions {
    public static partial class JsonExtensions {
         public static IEnumerable<JsonElement> SelectMany(this JsonDocument document,bool dispose=true)
             => document.RootElement.SelectMany().Finally(() => {
                 if (dispose) {
                     document.Dispose();
                 }
             });
         public static IEnumerable<JsonElement> SelectMany(this JsonElement jsonElement) 
             => jsonElement.ValueKind == JsonValueKind.Array ? jsonElement.EnumerateArrayUnboxed() : jsonElement.YieldItem();

         public static IEnumerable<JsonElement> EnumerateArrayUnboxed(this JsonElement tokenOrArray) {
             using var enumerator = tokenOrArray.EnumerateArray();
             while (enumerator.MoveNext()) {
                 yield return enumerator.Current;
             }
         }
         public static IEnumerable<JsonElement> EnumerateArray(this JsonElement tokenOrArray,string propertyName) 
             => tokenOrArray.GetProperty(propertyName).EnumerateArrayUnboxed();

         public static IEnumerable<JsonProperty> EnumerateObjectUnboxed(this JsonElement tokenOrArray,[CallerMemberName]string caller="") {
             using var enumerator = tokenOrArray.EnumerateObject();
             while (enumerator.MoveNext()) {
                 yield return enumerator.Current;
             }
         }

         public static IEnumerable<JsonProperty> GetProperties(this JsonElement element, string name) {
             using var enumerator = element.GetProperty(name).EnumerateObject();
             while (enumerator.MoveNext()) {
                 yield return enumerator.Current;
             }
         }

         public static T GetPropertyValue<T>(this JsonElement element, ReadOnlySpan<char> currentProperty,
             ReadOnlySpan<char> remainingProperties = default,Func<T> defaultValue=default,[CallerMemberName]string caller="") 
             => element.GetPropertyValue(currentProperty,defaultValue,remainingProperties,caller);
                
         public static ReadOnlySpan<char> GetPropertyValueAsSpan(this JsonElement element, ReadOnlySpan<char> propertyName) 
             => element.TryGetProperty(propertyName, out JsonElement property) &&
                property.ValueKind == JsonValueKind.String ? property.GetString().AsSpan() : ReadOnlySpan<char>.Empty;
                
         public static ReadOnlyMemory<char> GetPropertyValueAsMemory(this JsonElement element, ReadOnlySpan<char> propertyName)
             => element.TryGetProperty(propertyName, out JsonElement property) &&
                property.ValueKind == JsonValueKind.String ? property.GetString().AsMemory() : ReadOnlyMemory<char>.Empty;


         public static T GetPropertyValue<T>(this JsonElement element, ReadOnlySpan<char> currentProperty,Func<T> defaultValue,
             ReadOnlySpan<char> remainingProperties = default,[CallerMemberName]string caller="") {
             defaultValue ??= () => default;
             if (currentProperty.IsEmpty) return defaultValue();
             var dotIndex = remainingProperties.IndexOf('.');
             var nextProperty = dotIndex == -1 ? remainingProperties : remainingProperties.Slice(0, dotIndex);
             var nextRemainingProperties = dotIndex == -1 ? ReadOnlySpan<char>.Empty : remainingProperties.Slice(dotIndex + 1);
             if (!element.TryGetProperty(currentProperty, out var property)) return defaultValue();
             if (nextProperty.IsEmpty) {
                 switch (property.ValueKind){
                     case JsonValueKind.Number when typeof(T) == typeof(int):
                         return (T)(object)property.GetInt32();
                     case JsonValueKind.Number when typeof(T) == typeof(decimal):
                         return (T)(object)property.GetDecimal();
                     case JsonValueKind.Number when typeof(T) == typeof(double):
                         return (T)(object)property.GetDouble();
                     case JsonValueKind.Number when typeof(T) == typeof(float):
                         return (T)(object)property.GetSingle();
                     case JsonValueKind.Number when typeof(T) == typeof(long):
                         return (T)(object)property.GetInt64();
                     case JsonValueKind.Number when typeof(T) == typeof(ushort):
                         return (T)(object)property.GetUInt16();
                     case JsonValueKind.Number when typeof(T) == typeof(uint):
                         return (T)(object)property.GetUInt32();
                     case JsonValueKind.Number when typeof(T) == typeof(ulong):
                         return (T)(object)property.GetUInt64();
                     case JsonValueKind.String when typeof(T) == typeof(string):
                         return (T)(object)property.GetString();
                     case JsonValueKind.True when typeof(T) == typeof(bool):
                         return (T)(object)true;
                     case JsonValueKind.False when typeof(T) == typeof(bool):
                         return (T)(object)false;
                 }
             }
             return property.GetPropertyValue<T>(nextProperty, remainingProperties:nextRemainingProperties);
         }
    }
}