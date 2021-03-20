using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static String XmlPrint(this String xml) {
            if (string.IsNullOrEmpty(xml))
                return xml;
            String result = "";

            var mStream = new MemoryStream();
            var writer = new XmlTextWriter(mStream, Encoding.Unicode);
            var document = new XmlDocument();

            try {
                document.LoadXml(xml);
                writer.Formatting = Formatting.Indented;
                document.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();
                mStream.Position = 0;
                var sReader = new StreamReader(mStream);
                var formattedXml = sReader.ReadToEnd();

                result = formattedXml;
            }
            catch (XmlException) { }

            mStream.Close();
            writer.Close();

            return result;
        }
    }
}