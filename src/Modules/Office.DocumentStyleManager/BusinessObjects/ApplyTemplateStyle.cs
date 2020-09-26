using System.ComponentModel;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using JetBrains.Annotations;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.StyleTemplateService;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects{
	[DomainComponent]
	[Appearance(nameof(ApplyTemplateService.ApplyTemplate),AppearanceItemType.Action, nameof(Template) + " Is Null", Enabled = false,
		TargetItems = nameof(ApplyTemplateService.ApplyTemplate))]
	public class ApplyTemplateStyle:INotifyPropertyChanged,IObjectSpaceLink{
		private byte[] _original;
		private byte[] _changed;
		private DocumentStyleLinkTemplate _template;

		public ApplyTemplateStyle(){
			Documents=new BindingList<TemplateDocument>();
			ChangedStyles=new BindingList<DocumentStyleLink>();
		}

		public event PropertyChangedEventHandler PropertyChanged;
		[Browsable(false)]
		public IObjectSpace ObjectSpace{ get; set; }

		[NotifyPropertyChangedInvocator]
		[UsedImplicitly]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null){
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		[Browsable(false)]
		public string ListView{ get; set; }
		
		[CollectionOperationSet(AllowRemove = false)]
		public BindingList<TemplateDocument> Documents{ get; }

		[EditorAlias(EditorAliases.RichTextPropertyEditor)]
		public byte[] Original{
			get => _original;
			set{
				if (Equals(value, _original)) return;
				_original = value;
				OnPropertyChanged();
			}
		}

		[EditorAlias(EditorAliases.RichTextPropertyEditor)]
		public byte[] Changed{
			get => _changed;
			set{
				if (Equals(value, _changed)) return;
				_changed = value;
				OnPropertyChanged();
			}
		}

		[CollectionOperationSet(AllowAdd = false,AllowRemove = false)]
		public BindingList<DocumentStyleLink> ChangedStyles{ get; }


		public DocumentStyleLinkTemplate Template{
			get => _template;
			set{
				if (Equals(value, _template)) return;
				_template = value;
				OnPropertyChanged();
			}
		}
	}
}