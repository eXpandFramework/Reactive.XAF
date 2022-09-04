using System;
using System.ComponentModel;
using System.IO;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.Extensions.XAF.Xpo.BaseObjects {
    [DefaultProperty("FileName")]
    public class FileLinkObject : CustomBaseObject, IFileData, IEmptyCheckable, ISupportFullName {
        public FileLinkObject(Session session) : base(session) { }
        #region IFileData Members
        [Size(260), Custom("AllowEdit", "False")]
        public string FileName {
            get => GetPropertyValue<string>();
            set => SetPropertyValue("FileName", value); 
        }
        void IFileData.Clear() {
            Size = 0;
            FileName = string.Empty;
        }
        
        void IFileData.LoadFromStream(string fileName, Stream source) {
            Size = (int)source.Length;
            FileName = fileName;
        }
        void IFileData.SaveToStream(Stream destination) {
            try {
                if (destination == null)
                    OpenFileWithDefaultProgram(FullName);
                else
                    CopyFileToStream(FullName, destination);
            } catch (Exception exc) {
                throw new UserFriendlyException(exc);
            }
        }

        static void CopyFileToStream(string sourceFileName, Stream destination) {
            if (string.IsNullOrEmpty(sourceFileName) || destination == null) return;
            using Stream source = File.OpenRead(sourceFileName);
            source.CopyTo(destination);
        }
        static void OpenFileWithDefaultProgram(string sourceFileName) {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.FileName = sourceFileName;
            process.Start();
        }

        [Persistent]
        public int Size {
            get => GetPropertyValue<int>();
            private set => SetPropertyValue("Size", value);
        }
        #endregion
        #region IEmptyCheckable Members
        public bool IsEmpty => !File.Exists(FullName);

        #endregion
        #region ISupportFullName Members
        [Size(260), Custom("AllowEdit", "False")]
        public string FullName {
            get => GetPropertyValue<string>();
            set => SetPropertyValue("FullName", value);
        }
        #endregion
    }
}