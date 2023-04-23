using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fasterflect;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.TypeExtensions {
    
    [Flags]
    public enum Conversion {
        GuessValues = 0,
        None=1,
        TreatNullAsDefault = 2,
        TreatWhitespaceAsDefault = 4,
    }

    public static class XpandConvert {
        private const string ImplicitOperatorMethodName = "op_Implicit";
        private const string ExplicitOperatorMethodName = "op_Explicit";
        private static readonly CultureInfo DefaultCultureInfo = CultureInfo.CurrentCulture;
        private const Conversion DefaultConversion = Conversion.None|Conversion.TreatNullAsDefault|Conversion.TreatWhitespaceAsDefault;

        public static bool CanChange<T>(object value) {
            return TryToChange(value, out T _);
        }

        public static bool CanChange<T>(string value, CultureInfo culture) {
            return TryToChange(value, out T _, culture);
        }

        public static bool CanChange<T>(object value, Conversion options) {
            return TryToChange(value, out T _, options);
        }

        public static bool CanChange<T>(object value, CultureInfo culture, Conversion options) {
            return TryToChange(value, out T _, culture, options);
        }

        public static bool TryToChange<T>(object value, out T result) {
            return TryToChange(value, out result, DefaultCultureInfo);
        }

        public static bool TryToChange<T>(object value, out T result, CultureInfo culture) {
            return TryToChange(value, out result, culture, DefaultConversion);
        }

        public static bool TryToChange<T>(object value, out T result, Conversion options) {
            return TryToChange(value, out result, DefaultCultureInfo, options);
        }

        public static bool TryToChange<T>(object value, out T result, CultureInfo culture, Conversion options) {
            if (TryToChange(value, typeof(T), out var tmpResult, options, culture)) {
                result = (T)tmpResult;
                return true;
            }
            result = default(T);
            return false;
        }
        
        public static T Change<T>(this object value) 
            => value.Change<T>( DefaultCultureInfo);
        
        public delegate bool TryParseDelegate<T>(ReadOnlySpan<char> span, out T result);

        public static T Change<T>(this object value, CultureInfo culture) 
            => value.Change<T>( culture, DefaultConversion);

        public static T Change<T>(this object value, Conversion options) 
            => value.Change<T>( DefaultCultureInfo, options);

        public static T Change<T>(this object value, CultureInfo culture, Conversion options) 
            => (T)value.Change( typeof(T), culture, options);

        public static bool CanChange(this object value, Type destinationType) 
            => value.TryToChange( destinationType, out _);

        public static bool CanChange(this object value, Type destinationType, CultureInfo culture) 
            => value.TryToChange( destinationType, out _, culture);

        public static bool CanChange(this object value, Type destinationType, Conversion options) 
            => value.TryToChange( destinationType, out _, options);

        public static bool CanChange(this object value, Type destinationType, CultureInfo culture, Conversion options) 
            => value.TryToChange( destinationType, out _, options, culture);

        public static bool TryToChange(this object value, Type destinationType, out object result) 
            => value.TryToChange( destinationType, out result, DefaultCultureInfo);

        public static bool TryToChange(this object value, Type destinationType, out object result, CultureInfo culture) 
            => value.TryToChange( destinationType, out result, DefaultConversion, culture);

        public static bool TryToChange(this object value, Type destinationType, out object result, Conversion options)
            => value.TryToChange( destinationType, out result, options, DefaultCultureInfo);

        public static bool TryToChange(this object value, Type destinationType, out object result, Conversion options , CultureInfo culture) {
            if (destinationType == typeof(object)) {
                result = value;
                return true;
            }
            if (value.IsValueNull()) {
                return TryChangeFromNull(destinationType, out result, options);
            }
            if (destinationType.IsInstanceOfType(value)) {
                result = value;
                if (destinationType==typeof(string)&&IsNullString(value.ToString())) {
                    result = null;
                }
                return true;
            }

            if (destinationType == typeof(string)) {
                result = $"{value}";
                return true;
            }
            Type coreDestinationType = destinationType.IsGenericType ? destinationType.GenericTypeArguments.First() : destinationType;
            object tmpResult = null;
            if (TryChangeCore(value, coreDestinationType, ref tmpResult, culture, options)) {
                result = tmpResult;
                return true;
            }
            result = null;
            return false;
        }

        private static bool TryChangeFromNull(Type destinationType, out object result, Conversion options) {
            result = destinationType.DefaultValue();
            if (result == null) {
                return true;
            }
            return (options & Conversion.TreatNullAsDefault) ==
                   Conversion.TreatNullAsDefault;
        }

        [SuppressMessage("ReSharper", "NonConstantEqualityExpressionHasConstantResult")]
        private static bool TryChangeCore(object value, Type destinationType, ref object result, CultureInfo culture, Conversion options) {
            if (value.GetType() == destinationType) {
                result = value;
                return true;
            }
            if (value is JsonValue jsonValue) {
                var valueKind = jsonValue.GetValue<JsonElement>().ValueKind;
                if (valueKind == JsonValueKind.String&&destinationType!=typeof(string)) {
                    return jsonValue.Deserialize<string>().TryToChange(destinationType,out result,options,culture);    
                }
                result = jsonValue.Deserialize(destinationType);
                return true;
            }
            if (TryChangeByTryParse(value.ToString(), destinationType, ref result)) {
                return true;
            }
            if (TryChangeExplicit(value, destinationType, ExplicitOperatorMethodName, ref result)) {
                return true;
            }
            if (TryChangeExplicit(value, destinationType, ImplicitOperatorMethodName, ref result)) {
                return true;
            }
            if (TryChangeByIntermediateConversion(value, destinationType, ref result, culture, options)) {
                return true;
            }
            if (destinationType.IsEnum) {
                if (TryChangeToEnum(value, destinationType, ref result)) {
                    return true;
                }
            }

            var typeConverter = TypeDescriptor.GetConverter(value.GetType());
            if (typeConverter.CanConvertFrom(destinationType)) {
	            result = typeConverter.ConvertFrom(value);
	            return true;
            }

            if ((options & Conversion.TreatWhitespaceAsDefault) == Conversion.TreatWhitespaceAsDefault) {
                if (value is string s && s.IsWhiteSpace()) {
                    result = destinationType.DefaultValue();
                    return true;
                }
            }
            if ((options & Conversion.GuessValues) == Conversion.GuessValues) {
                if (TryChangeGuessedValues(value, destinationType, ref result)) {
                    return true;
                }
            }

            return false;
        }

        static bool TryChangeNullString(object value, Type destinationType, ref object result) 
            => IsNullString($"{value}") && TryToChange(null, destinationType, out result, Conversion.TreatNullAsDefault);

        static bool IsNullString(string asString) 
            => asString != String.Empty && string.CompareOrdinal("(NULL)", asString) == 0;

        private static bool TryChangeGuessedValues(object value, Type destinationType, ref object result) {
            if (value is char c && destinationType == typeof(bool)) {
                return TryChangeCharToBool(c, ref result);
            }
            var changeNullString = TryChangeNullString(value, destinationType, ref result);
            if (changeNullString)
                return true;
            if ( destinationType == typeof(bool)) {
                return TryChangeStringToBool(value.ToString(), ref result);
            }

            if (value is bool b && destinationType == typeof(char)) {
                return ChangeBoolToChar(b, out result);
            }
            if (destinationType ==typeof(TimeSpan)) {
                if (TryToChange(value, typeof (long), out var o,Conversion.GuessValues)) {
                    result = new TimeSpan((long) o);
                    return true;
                }
            }

            if ((destinationType == typeof (Rectangle)) &&!string.IsNullOrEmpty(value + "")){
                var rectParts =$"{value}".Split(';');
                if (rectParts.Length == 2){
                    var canChange = rectParts[0].TryToChange(typeof (Point), out var point);
                    if (canChange){
                        canChange=rectParts[1].TryToChange(typeof (Size), out var size);
                        if (canChange){
                            result = new Rectangle((Point) point, (Size) size);
                            return true;
                        }
                    }
                }
            }

            if ((destinationType == typeof(Size) || destinationType == typeof(Point)) && !string.IsNullOrEmpty(value + "")) {
                var strings = $"{value}".Split('x');
                if (strings.Length == 2 && (strings[0].CanChange(typeof (int)) && strings[1].CanChange(typeof (int)))){
                    result = destinationType.CreateInstance(int.Parse(strings[0]), int.Parse(strings[1]));
                    return true;
                }
            }

            return false;
        }

        private static bool TryChangeCharToBool(char value, ref object result) {
            if ("1JYT".Contains(value.ToString(CultureInfo.InvariantCulture).ToUpper())) {
                result = true;
                return true;
            }
            if ("0NF".Contains(value.ToString(CultureInfo.InvariantCulture).ToUpper())) {
                result = false;
                return true;
            }
            return false;
        }

        private static bool TryChangeStringToBool(string value, ref object result) {
            var trueValues = new List<string>(new[] { "1", "yes", "true" });
            if (trueValues.Contains(value.Trim().ToLower())) {
                result = true;
                return true;
            }
            var falseValues = new List<string>(new[] { "0", "no", "false" });
            if (falseValues.Contains(value.Trim().ToLower())) {
                result = false;
                return true;
            }
            return false;
        }

        private static bool ChangeBoolToChar(bool value, out object result) {
            result = value ? 'T' : 'F';
            return true;
        }

        private static bool TryChangeByTryParse(string value, Type destinationType, ref object result) {
            if (destinationType == typeof(Boolean)) {
                return TryParseBool(value, ref result);
            }
            if (destinationType == typeof(Byte)) {
                return TryParseByte(value, ref result);
            }
            if (destinationType == typeof(Char)) {
                return TryParseChar(value, ref result);
            }
            if (destinationType == typeof(DateTime)) {
                return TryParseDateTime(value, ref result);
            }
            if (destinationType == typeof(Decimal)) {
                return TryParseDecimal(value, ref result);
            }
            if (destinationType == typeof(Double)) {
                return TryParseDouble(value, ref result);
            }
            if (destinationType == typeof(Int16)) {
                return TryParseInt16(value, ref result);
            }
            if (destinationType == typeof(Int32)) {
                return TryParseInt32(value, ref result);
            }
            if (destinationType == typeof(Int64)) {
                return TryParseInt64(value, ref result);
            }
            if (destinationType == typeof(SByte)) {
                return TryParseSByte(value, ref result);
            }
            if (destinationType == typeof(Single)) {
                return TryParseSingle(value, ref result);
            }
            if (destinationType == typeof(UInt16)) {
                return TryParseUInt16(value, ref result);
            }
            if (destinationType == typeof(UInt32)) {
                return TryParseUInt32(value, ref result);
            }
            if (destinationType == typeof(UInt64)) {
                return TryParseUInt64(value, ref result);
            }
            if (destinationType == typeof(Guid)) {
                return TryParseUGuid(value, ref result);
            }
            return false;
        }

        static bool TryParseUGuid(string valueString, ref object result) {
            var tryParse = Guid.TryParse(valueString, out var guid);
            if (tryParse)
                result = guid;
            return tryParse;
        }

        static bool TryParseUInt64(string valueString, ref object result) {
            var tryParse = UInt16.TryParse(valueString, out var @ushort);
            if (tryParse)
                result = @ushort;
            return tryParse;
        }

        static bool TryParseUInt32(string valueString, ref object result) {
            var tryParse = UInt32.TryParse(valueString, out var u);
            if (tryParse)
                result = u;
            return tryParse;
        }

        static bool TryParseUInt16(string valueString, ref object result) {
            var tryParse = UInt16.TryParse(valueString, out var @ushort);
            if (tryParse)
                result = @ushort;
            return tryParse;
        }

        static bool TryParseSingle(string valueString, ref object result) {
            var tryParse = Single.TryParse(valueString, out var f);
            if (tryParse)
                result = f;
            return tryParse;
        }

        static bool TryParseSByte(string valueString, ref object result) {
            var tryParse = SByte.TryParse(valueString, out var @sbyte);
            if (tryParse)
                result = @sbyte;
            return tryParse;
        }

        static bool TryParseInt64(string valueString, ref object result) {
            var tryParse = Int64.TryParse(valueString, out var l);
            if (tryParse)
                result = l;
            return tryParse;
        }

        static bool TryParseInt32(string valueString, ref object result) {
            var tryParse = Int32.TryParse(valueString, out var i);
            if (tryParse)
                result = i;
            return tryParse;
        }

        static bool TryParseInt16(string valueString, ref object result) {
            var tryParse = Int16.TryParse(valueString, out var s);
            if (tryParse)
                result = s;
            return tryParse;
        }

        static bool TryParseDouble(string valueString, ref object result) {
            var tryParse = double.TryParse(valueString, out var d);
            if (tryParse)
                result = d;
            return tryParse;
        }

        static bool TryParseDecimal(string value, ref object result) {
            var tryParse = decimal.TryParse(value,NumberStyles.Float, CultureInfo.InvariantCulture, out var @decimal);
            if (tryParse)
                result = @decimal;
            return tryParse;
        }

        static bool TryParseDateTime(string value, ref object result) {
            var tryParse = DateTime.TryParse(value, out var time);
            if (tryParse)
                result = time;
            return tryParse;
        }

        static bool TryParseChar(string value, ref object result) {
            var tryParse = char.TryParse(value, out var c);
            if (tryParse)
                result = c;
            return tryParse;
        }

        static bool TryParseByte(string value, ref object result) {
            var tryParse = byte.TryParse(value, out var b);
            if (tryParse)
                result = b;
            return tryParse;
        }

        static bool TryParseBool(string value, ref object result) {
            var tryParse = bool.TryParse(value, out var b);
            if (tryParse)
                result = b;
            return tryParse;
        }

        private static bool TryChangeExplicit(object value, Type destinationType, string operatorMethodName, ref object result) 
            => TryChangeExplicit(value, value.GetType(), destinationType, operatorMethodName, ref result) ||
               TryChangeExplicit(value, destinationType, destinationType, operatorMethodName, ref result);

        private static bool TryChangeExplicit(object value, Type invokerType, Type destinationType, string explicitMethodName, ref object result) {
            var methods = invokerType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (MethodInfo method in methods.Where(m => m.Name == explicitMethodName)) {
                if (destinationType.IsAssignableFrom(method.ReturnType)) {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType == value.GetType()) {
                        try {
                            result = method.Invoke(null, new[] { value });
                            return true;
                        }
                        catch {
                            // ignored
                        }
                    }
                }
            }
            return false;
        }

        private static bool TryChangeByIntermediateConversion(object value, Type destinationType, ref object result, CultureInfo culture, Conversion options) {
            if (value is char
                && (destinationType == typeof(double) || destinationType == typeof(float))) {
                return TryChangeCore(Convert.ToInt16(value), destinationType, ref result, culture, options);
            }
            if ((value is double || value is float) && destinationType == typeof(char)) {
                return TryChangeCore(Convert.ToInt16(value), destinationType, ref result, culture, options);
            }
            return false;
        }

        private static bool TryChangeToEnum(object value, Type destinationType, ref object result) {
            var enumTryParse = destinationType.EnumTryParse(value.ToString(), out var o);
            if (enumTryParse)
                result = o;
            return enumTryParse;
        }

        public static object Change(this object value, Type destinationType) 
            => value.Change( destinationType, DefaultCultureInfo);

        public static object Change(this object value, Type destinationType, CultureInfo culture) 
            => value.Change( destinationType, culture, DefaultConversion);

        public static object Change(this object value, Type destinationType, Conversion options) 
            => value.Change( destinationType, DefaultCultureInfo, options);

        public static object Change(this object value, Type destinationType, CultureInfo culture, Conversion options) {
            if (TryToChange(value, destinationType, out var result, options, culture)) {
                return result;
            }
            throw new TypeCannotChanged(value, destinationType);
        }
    }
    
    public class TypeCannotChanged : InvalidOperationException {
        public TypeCannotChanged(object valueToConvert, Type destinationType)
            : base($"'{valueToConvert}' ({valueToConvert?.GetType()}) is not convertible to '{destinationType}'.") {
        }
    }

}

