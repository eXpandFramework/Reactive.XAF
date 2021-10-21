using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RazorLight.Razor;

namespace Xpand.XAF.Modules.ObjectTemplate.Template {
    public sealed class ObjectTemplateRazorProject : RazorLightProject {
        public const string DefaultExtension = ".cshtml";
        // private readonly IFileProvider _fileProvider;

        // public NotificationRazorProject(string root)
        //     : this(root, ".cshtml") { }

        // public NotificationRazorProject(string root, string extension) {
        //     Extension = extension ?? throw new ArgumentNullException(nameof(extension));
        //     Root = Directory.Exists(root)
        //         ? root
        //         : throw new DirectoryNotFoundException("Root directory " + root + " not found");
        //     _fileProvider = new PhysicalFileProvider(Root);
        // }

        public string Extension { get; set; }

        /// <summary>
        /// Looks up for the template source with a given <paramref name="templateKey" />
        /// </summary>
        /// <param name="templateKey">Unique template key</param>
        /// <returns></returns>
        /// <footer><a href="https://www.google.com/search?q=RazorLight.Razor.FileSystemRazorProject.GetItemAsync">`FileSystemRazorProject.GetItemAsync` on google.com</a></footer>
        public override Task<RazorLightProjectItem> GetItemAsync(string templateKey) {
            if (!templateKey.EndsWith(Extension))
                templateKey += Extension;
            string fileName = templateKey;
            // string fileName = NormalizeKey(templateKey);
            var razorProjectItem = new NotificationRazorProjectItem(templateKey, new FileInfo(fileName));
            // if (razorProjectItem.Exists)
                // razorProjectItem.ExpirationToken=NullChangeToken.Singleton;
                // razorProjectItem.ExpirationToken = _fileProvider.Watch(templateKey);
            return Task.FromResult((RazorLightProjectItem)razorProjectItem);
        }

        // /// <summary>Root folder</summary>
        // /// <footer><a href="https://www.google.com/search?q=RazorLight.Razor.FileSystemRazorProject.Root">`FileSystemRazorProject.Root` on google.com</a></footer>
        // public string Root { get; }

        // private string NormalizeKey(string templateKey) {
        //     string str = !string.IsNullOrEmpty(templateKey)
        //         ? templateKey
        //         : throw new ArgumentNullException(nameof(templateKey));
        //     if (!str.StartsWith(Root, StringComparison.OrdinalIgnoreCase)) {
        //         if (templateKey[0] == '/' || templateKey[0] == '\\')
        //             templateKey = templateKey.Substring(1);
        //         str = Path.Combine(Root, templateKey);
        //     }
        //
        //     return str.Replace('\\', '/');
        // }

        public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey) =>
            Task.FromResult(Enumerable.Empty<RazorLightProjectItem>());
    }

    public class NotificationRazorProjectItem : RazorLightProjectItem {
        public NotificationRazorProjectItem(string templateKey, FileInfo fileInfo) {
            Key = templateKey;
            File = fileInfo;
        }

        public FileInfo File { get; }

        public override string Key { get; }

        public override bool Exists => File.Exists;

        public override Stream Read() => File.OpenRead();
    }
}