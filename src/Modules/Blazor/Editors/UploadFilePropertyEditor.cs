using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Fasterflect;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Xpand.Extensions.StreamExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using EditorAliases = Xpand.Extensions.XAF.Attributes.EditorAliases;

namespace Xpand.XAF.Modules.Blazor.Editors {
    public class UploadFileMiddleware {
        static readonly ISubject<(string name,byte[] bytes,string editor)> FormFileSubject=Subject.Synchronize(new Subject<(string name,byte[] bytes,string editor)>());
        public static IObservable<(string name, byte[] bytes,string editor)> FormFile => FormFileSubject.AsObservable();
        private readonly RequestDelegate _next;
        public UploadFileMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context) {
            string requestPath = context.Request.Path.Value.TrimStart('/');
            if(requestPath.StartsWith("api/Upload/UploadFile") ) {
                var formFile = context.Request.Form.Files.First();
                FormFileSubject.OnNext((formFile.FileName,formFile.OpenReadStream().Bytes(),context.Request.Query["Editor"]));
            }
            else {
                await _next(context);
            }
        }
    }

    [PropertyEditor(typeof(IEnumerable<IFileData>),EditorAliases.UploadFile, false)]
    public class UploadFilePropertyEditor : ComponentPropertyEditor,IComplexViewItem {
        readonly Guid _guid=Guid.NewGuid();
        readonly Subject<Unit> _upLoaded=new();
        public UploadFilePropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model) {}

        protected override void RenderComponent(RenderTreeBuilder builder) {
            builder.AddMarkupContent(0,@"
<div id=""overviewDemoDropZone"" class=""card custom-drop-zone jumbotron"">
    <svg class=""drop-file-icon mb-3"" role=""img"" style=""width: 42px; height: 42px;""><use href=""#drop-file-icon""></use></svg>
    <span>Drag and Drop File Here</span>
</div>
");
            builder.AddComponent(NewDxUpload(),1);

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "BL0005:Component parameter should not be set outside of its component.", Justification = "<Pending>")]
        protected DxUpload NewDxUpload() {
            var name = _guid.ToString();
            var dxUpload = new DxUpload {
                UploadUrl = $"/api/Upload/UploadFile?Editor={name}",
                Name = name,
                AllowMultiFileUpload = true,
                ExternalDropZoneCssSelector = "#overviewDemoDropZone",
                ExternalDropZoneDragOverCssClass = "custom-drag-over border-light text-white"
            };
            // dxUpload.FileUploaded += _ => _upLoaded.OnNext(Unit.Default);
            return dxUpload;
        }

        public void Setup(IObjectSpace objectSpace, XafApplication application) {
            UploadFileMiddleware.FormFile.Where(t => t.editor==_guid.ToString())
                .Buffer(_upLoaded)
                .SelectMany(list => list)
                .Do(formFile => {
                    var elementType = MemberInfo.ListElementType;
                    var fileData = (IFileData) objectSpace.CreateObject(elementType);
                    fileData.LoadFromStream(formFile.name,new MemoryStream(formFile.bytes));
                    PropertyValue.CallMethod("BaseAdd", fileData);
                    objectSpace.SetModified(CurrentObject);
                })
                .TakeUntil(objectSpace.WhenDisposed())
                .Subscribe();
        }
    }
}
