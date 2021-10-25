using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RazorLight.Razor;

namespace Xpand.XAF.Modules.ObjectTemplate.Template {
    sealed class ObjectTemplateRazorProject : RazorLightProject {
        private readonly MemoryStream _objectTemplate;
        public ObjectTemplateRazorProject(MemoryStream template) => _objectTemplate = template;

        public override Task<RazorLightProjectItem> GetItemAsync(string templateKey) 
            => Task.FromResult((RazorLightProjectItem)new NotificationRazorProjectItem(templateKey, _objectTemplate));
        
        public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey) =>
            Task.FromResult(Enumerable.Empty<RazorLightProjectItem>());
    }

    class NotificationRazorProjectItem : RazorLightProjectItem {
        private readonly MemoryStream _stream;

        public NotificationRazorProjectItem(string templateKey, MemoryStream stream) {
            Key = templateKey;
            _stream = stream;
        }

        public override string Key { get; }

        public override bool Exists => true;

        public override Stream Read() => _stream;
    }
}