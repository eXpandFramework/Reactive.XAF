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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StreamExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using EditorAliases = Xpand.Extensions.XAF.Attributes.EditorAliases;

namespace Xpand.XAF.Modules.Blazor.Editors {
    public class UploadFileMiddleware(RequestDelegate next) {
        static readonly ISubject<(string name,byte[] bytes,string editor)> FormFileSubject=Subject.Synchronize(new Subject<(string name,byte[] bytes,string editor)>());
        public static IObservable<(string name, byte[] bytes,string editor)> FormFile => FormFileSubject.AsObservable();

        public async Task Invoke(HttpContext context) {
            string requestPath = context.Request.Path.Value!.TrimStart('/');
            if(requestPath.StartsWith("api/Upload/UploadFile") ) {
                var formFile = context.Request.Form.Files.First();
                FormFileSubject.OnNext((formFile.FileName,formFile.OpenReadStream().Bytes(),context.Request.Query["Editor"]));
            }
            else {
                await next(context);
            }
        }
    }

    [PropertyEditor(typeof(IEnumerable<IFileData>),EditorAliases.UploadFile, false)]
    public class UploadFilePropertyEditor(Type objectType, IModelMemberViewItem model)
        : ComponentPropertyEditor(objectType, model), IComplexViewItem {
        readonly Guid _guid=Guid.NewGuid();
        readonly Subject<Unit> _upLoaded=new();
        

        protected override void RenderComponent(RenderTreeBuilder builder) {
            builder.AddMarkupContent(0,@"
<div id=""overviewDemoDropZone"" class=""card custom-drop-zone jumbotron"">
    <svg class=""drop-file-icon mb-3"" role=""img"" style=""width: 42px; height: 42px;""><use href=""#drop-file-icon""></use></svg>
    <span>Drag and Drop File Here</span>
</div>
");
            RenderFragment dxUploadContent = CreateDxUploadContent();
            builder.AddContent(1, dxUploadContent);

        }

        private RenderFragment CreateDxUploadContent() => builder => {
            var name = _guid.ToString();
            builder.OpenComponent<DxUpload>(0);
            builder.AddAttribute(1, "UploadUrl", $"/api/Upload/UploadFile?Editor={name}");
            builder.AddAttribute(2, "Name", name);
            builder.AddAttribute(3, "AllowMultiFileUpload", true);
            builder.AddAttribute(4, "ExternalDropZoneCssSelector", "#overviewDemoDropZone");
            builder.AddAttribute(5, "ExternalDropZoneDragOverCssClass", "custom-drag-over border-light text-white");
            builder.AddAttribute(6, "FileUploaded", EventCallback.Factory.Create<FileUploadEventArgs>(this,_ => _upLoaded.OnNext()));
            builder.CloseComponent();
        };

        
        public void Setup(IObjectSpace objectSpace, XafApplication application)
            => UploadFileMiddleware.FormFile.Where(t => t.editor==_guid.ToString())
                .Buffer(_upLoaded).ObserveOnContext()
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
