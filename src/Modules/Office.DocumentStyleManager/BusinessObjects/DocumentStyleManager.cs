using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;

using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects{
	[DomainComponent]
	[Appearance("Enable_TemplateStylesAction_when_LinkTemplate_visiable", AppearanceItemType.Action,
		nameof(DocumentStyleLinkTemplate) + " Is Null", Enabled = false,
		TargetItems = nameof(TemplateStyleSelectionService.TemplateStyleSelection))]
	public class DocumentStyleManager:INotifyPropertyChanged,IObjectSpaceLink {
		private DocumentStyleLinkTemplate _documentStyleLinkTemplate;
		private byte[] _content;
		private byte[] _original;
		private int _position;
		private int _paragraph;

		public DocumentStyleManager(){
		   UsedStyles=new List<IDocumentStyle>();
		   UnusedStyles=new List<IDocumentStyle>();
		}

		[XafDisplayName("Template")]
		public DocumentStyleLinkTemplate DocumentStyleLinkTemplate{
			get => _documentStyleLinkTemplate;
			set{
				if (Equals(value, _documentStyleLinkTemplate)) return;
				_documentStyleLinkTemplate = value;
				OnPropertyChanged();
			}
		}

		[EditorAlias(EditorAliases.RichTextPropertyEditor)]
		public byte[] Content{
			get => _content;
			set{
				if (Equals(value, _content)) return;
				_content = value;
				OnPropertyChanged();
			}
		}

		[EditorAlias(EditorAliases.RichTextPropertyEditor)]
		public byte[] Original{
			get => _original;
			set{
				if (Equals(value, _original)) return;
				_original = value;
				OnPropertyChanged();
			}
		}

		public BindingList<DocumentStyle> AllStyles => new(ImmutableList.CreateRange(UsedStyles
		       .Concat(UnusedStyles).Cast<DocumentStyle>().Distinct()));

		[Browsable(false)]
		public List<IDocumentStyle> UnusedStyles{ get;  }
		
		
		public int Position{
			get => _position;
			set{
				if (value == _position) return;
				_position = value;
				OnPropertyChanged();
			}
		}

		
		public int Paragraph{
			get => _paragraph;
			set{
				if (value == _paragraph) return;
				_paragraph = value;
				OnPropertyChanged();
			}
		}

		[Browsable(false)]
		public List<IDocumentStyle> UsedStyles{ get;  }

		public BindingList<DocumentStyle> ReplacementStyles => new(ImmutableList.CreateRange(UsedStyles.Concat(UnusedStyles).Cast<DocumentStyle>().Distinct()));
		public event PropertyChangedEventHandler PropertyChanged;

		
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null){
		   PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		[Browsable(false)]
		public IObjectSpace ObjectSpace{ get; set; }
	}

	
}