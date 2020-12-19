using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using Fasterflect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.StreamExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Blazor.Editors.UploadFile {
    public class UploadFileMiddleware {
        static readonly ISubject<(string name,byte[] bytes,string editor)> FormFileSubject=Subject.Synchronize(new Subject<(string name,byte[] bytes,string editor)>());

        public static IObservable<(string name, byte[] bytes,string editor)> FormFile => FormFileSubject.AsObservable();

        private readonly RequestDelegate _next;
        public UploadFileMiddleware(RequestDelegate next) {
            this._next = next;
        }
        public async Task Invoke(HttpContext context,GlobalItems globalItems) {
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

    public class UploadFileModel : ComponentModelBase {
        internal Subject<Unit> UploadedSubject=new Subject<Unit>();
        public UploadFileModel(Guid guid) {
            Name = guid.ToString();
            UploadUrl = $"/api/Upload/UploadFile?Editor={Name}";
            AllowMultiFileUpload = true;
            DropZone = @"
<div id=""overviewDemoDropZone"" class=""card custom-drop-zone jumbotron"">
    <svg class=""drop-file-icon mb-3"" role=""img"" style=""width: 42px; height: 42px;""><use href=""#drop-file-icon""></use></svg>
    <span>Drag and Drop File Here</span>
</div>
";
        }

        public IObservable<Unit> Uploaded => UploadedSubject.AsObservable();

        public string Value {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }

        public bool ReadOnly {
            get => GetPropertyValue<bool>();
            set => SetPropertyValue(value);
        }

        public string UploadUrl {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }

        public string DropZone {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }

        public string Name {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }

        public int ChunkSize {
            get => GetPropertyValue<int>();
            set => SetPropertyValue(value);
        }
        public bool AllowMultiFileUpload {
            get => GetPropertyValue<bool>();
            set => SetPropertyValue(value);
        }
        public bool AllowCancel {
            get => GetPropertyValue<bool>();
            set => SetPropertyValue(value);
        }
        public bool AllowPause {
            get => GetPropertyValue<bool>();
            set => SetPropertyValue(value);
        }
    }

    public class UploadFileAdapter : ComponentAdapterBase {
        public UploadFileAdapter(UploadFileModel componentModel) 
            => ComponentModel = componentModel ?? throw new ArgumentNullException(nameof(componentModel));

        public UploadFileModel ComponentModel { get; }

        public override void SetAllowEdit(bool allowEdit) => ComponentModel.ReadOnly = !allowEdit;

        public override object GetValue() => ComponentModel.Value;

        public override void SetValue(object value) { }

        protected override RenderFragment CreateComponent() => ComponentModelObserver.Create(ComponentModel, UploadFileComponent.Create(ComponentModel));

        public override void SetAllowNull(bool allowNull) { }

        public override void SetDisplayFormat(string displayFormat) { }

        public override void SetEditMask(string editMask) { }

        public override void SetEditMaskType(EditMaskType editMaskType) { }

        public override void SetErrorIcon(ImageInfo errorIcon) { }

        public override void SetErrorMessage(string errorMessage) { }

        public override void SetIsPassword(bool isPassword) { }

        public override void SetMaxLength(int maxLength) { }

        public override void SetNullText(string nullText) { }
    }

    [PropertyEditor(typeof(IEnumerable<IFileData>),nameof(UploadFilePropertyEditor), false)]
    public class UploadFilePropertyEditor : BlazorPropertyEditorBase,IComplexViewItem {
        readonly Guid _guid=Guid.NewGuid();
        private readonly UploadFileAdapter _uploadFileAdapter;

        public UploadFilePropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model) {
            _uploadFileAdapter = new UploadFileAdapter(new UploadFileModel(_guid));
        }

        protected override IComponentAdapter CreateComponentAdapter() => _uploadFileAdapter;

        public void Setup(IObjectSpace objectSpace, XafApplication application) {
            UploadFileMiddleware.FormFile.Where(t => t.editor==_guid.ToString())
                .Buffer(_uploadFileAdapter.ComponentModel.Uploaded)
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