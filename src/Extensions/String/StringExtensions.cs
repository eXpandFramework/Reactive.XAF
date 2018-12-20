﻿using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Fasterflect;

namespace DevExpress.XAF.Extensions.String {
    /// <summary>
    /// Summary description for StringHelper.
    /// </summary>
    public static class StringExtensions {
        private static readonly Regex _isGuid =
            new Regex(
                @"^(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}$",
                RegexOptions.Compiled);

        public static string XMLEncode(this string value) {
            return value.TrimEnd((char)1).Replace("&", "&amp;").Replace("'", "&apos;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        public static System.String XMLPrint(this System.String xml){
            if (string.IsNullOrEmpty(xml))
                return xml;
            System.String result = "";

            var mStream = new MemoryStream();
            var writer = new XmlTextWriter(mStream, Encoding.Unicode);
            var document = new XmlDocument();

            try{
                document.LoadXml(xml);
                writer.Formatting = Formatting.Indented;
                document.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();
                mStream.Position = 0;
                var sReader = new StreamReader(mStream);
                System.String formattedXML = sReader.ReadToEnd();

                result = formattedXML;
            }
            catch (XmlException){
            }

            mStream.Close();
            writer.Close();

            return result;
        }

        public static string XMLDecode(this string value) {
            return value.Replace("&amp;", "&").Replace("&apos;", "'").Replace("&quot;", "\"").Replace("&lt;", "<").Replace("&gt;", ">");
        }
        public static System.String RemoveDiacritics(this System.String s) {
            System.String normalizedString = s.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (char c in normalizedString) {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }

            return stringBuilder.ToString();
        }

        public static string GetAttributeValue(this XElement element, XName name) {
            XAttribute xAttribute = element.Attribute(name);
            return xAttribute?.Value;
        }

        public static string MakeFirstCharUpper(this string s) {
            if ((s + "").Length > 0) {
                string substring1 = s.Substring(0, 1).ToUpper();
                string substring2 = s.Substring(1);
                return substring1 + substring2;
            }
            return s;
        }

        public static string MakeFirstCharLower(this string s) {
            if ((s + "").Length > 0) {
                string substring1 = s.Substring(0, 1).ToLower();
                string substring2 = s.Substring(1);
                return substring1 + substring2;
            }
            return s;
        }

        public static string Inject(this string injectToString, int positionToInject, string stringToInject) {
            var builder = new StringBuilder();
            builder.Append(injectToString.Substring(0, positionToInject));
            builder.Append(stringToInject);
            builder.Append(injectToString.Substring(positionToInject));
            return builder.ToString();
        }

        public static long Val(this string value) {
            string returnVal = System.String.Empty;
            MatchCollection collection = Regex.Matches(value, "\\d+");
            returnVal = collection.Cast<Match>().Aggregate(returnVal, (current, match) => current + match.ToString());
            return Convert.ToInt64(returnVal);
        }

        public static bool IsGuid(this string candidate) {
            if (candidate != null) {
                if (_isGuid.IsMatch(candidate)) {
                    return true;
                }
            }
            return false;
        }

        public static bool IsValueNull(this object value) {
            return value == null || value == DBNull.Value;
        }

        public static object GetDefaultValue(this Type type) {
            return type.IsValueType ? type.CreateInstance() : null;
        }

        public static bool IsGeneric(this Type type) {
            return type.IsGenericType &&
                   type.GetGenericTypeDefinition() == typeof(Nullable<>).GetGenericTypeDefinition();
        }

        public static Type GetUnderlyingType(this Type type) {
            return Nullable.GetUnderlyingType(type);
        }

        public static bool IsWhiteSpace(this string value) {
            return value.All(Char.IsWhiteSpace);
        }

        public static string CleanCodeName(string name) {
            var regex = new Regex(@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]");
            string ret = regex.Replace(name + "", "");
            if (!(System.String.IsNullOrEmpty(ret)) && !Char.IsLetter(ret, 0) && !CodeDomProvider.CreateProvider("C#").IsValidIdentifier(ret))
                ret = System.String.Concat("_", ret);
            return ret;
        }
    }
}