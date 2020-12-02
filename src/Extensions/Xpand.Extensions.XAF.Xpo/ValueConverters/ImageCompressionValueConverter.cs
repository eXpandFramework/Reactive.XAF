using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using DevExpress.Xpo.Metadata;
using Xpand.Extensions.StreamExtensions;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters {
    public class ImageCompressionValueConverter : ValueConverter{
        #region Properties

        public override Type StorageType => typeof (byte[]);

        #endregion

        public override object ConvertToStorageType(object value){
            if (value != null && !(value is Image)){
                throw new ArgumentException();
            }
            if (value == null){
                return null;
            }
            var ms = new MemoryStream();
            ((Image) value).Save(ms, ImageFormat.Jpeg);
            return ms.GZip();
        }

        public override object ConvertFromStorageType(object value) {
            if (value != null && !(value is byte[])){
                throw new ArgumentException();
            }

            return value == null || ((byte[]) value).Length == 0 ? value : new MemoryStream((byte[]) value).UnGzip();
        }
    }
}