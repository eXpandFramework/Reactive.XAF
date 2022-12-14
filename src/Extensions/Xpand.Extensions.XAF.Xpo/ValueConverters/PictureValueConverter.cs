using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using DevExpress.Xpo.Metadata;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters {
    public class PictureValueConverter : ValueConverter {
        public override Type StorageType => typeof(byte[]);

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        public override object ConvertToStorageType(object value) {
            if (value == null) {
                return null;
            }

            var m = new MemoryStream();
            ((Image) value).Save(m, ImageFormat.Jpeg);
            return m.GetBuffer();
        }

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        public override object ConvertFromStorageType(object value) {
            if (value == null) {
                return null;
            }

            var m = new MemoryStream((byte[]) value);
            return new Bitmap(m);
        }
    }
}