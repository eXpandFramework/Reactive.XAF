using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using DevExpress.XtraRichEdit.API.Native;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects{
	[Appearance("Used",AppearanceItemType.ViewItem,"["+nameof(Count)+"] > 0",Context = nameof(ViewType.ListView),TargetItems = "*",FontColor = "DarkGreen")]
	public class DocumentStyleLink:CustomBaseObject{

		public DocumentStyleLink(Session session) : base(session){
		}

		DocumentStyleLinkTemplate _documentStyleLinkTemplate;

		[Association("DocumentStyleLinkTemplate-DocumentStyleLinks")][RuleRequiredField]
		public DocumentStyleLinkTemplate DocumentStyleLinkTemplate{
			get => _documentStyleLinkTemplate;
			set => SetPropertyValue(nameof(DocumentStyleLinkTemplate), ref _documentStyleLinkTemplate, value);
		}

		int _count;
		[VisibleInListView(false)][VisibleInDetailView(false)][VisibleInLookupListView(false)]
		public int Count{
			get => _count;
			set => SetPropertyValue(nameof(Count), ref _count, value);
		}

		public void SetDefaultPropertiesProvider(Document defaultPropertiesProvider){
			_defaultPropertiesProvider = defaultPropertiesProvider;
			_originalStyle = Original?.ToDocumentStyle(_defaultPropertiesProvider);
			_replacementStyle=Replacement?.ToDocumentStyle(_defaultPropertiesProvider);
		}

		[XafDisplayName(nameof(Original))]
		public DocumentStyle OriginalStyle => _originalStyle;

		[XafDisplayName(nameof(Replacement))]
		public DocumentStyle ReplacementStyle => _replacementStyle;
		

		TemplateStyle _original;
		[RuleRequiredField][Browsable(false)]
		public TemplateStyle Original{
			get => _original;
			set => SetPropertyValue(nameof(Original), ref _original, value);
		}

		TemplateStyle _replacement;
		private Document _defaultPropertiesProvider;

		[RuleRequiredField(TargetCriteria = nameof(Operation)+"'"+nameof(DocumentStyleLinkOperation.Replace)+"'")][Browsable(false)]
		public TemplateStyle Replacement{
			get => _replacement;
			set => SetPropertyValue(nameof(Replacement), ref _replacement, value);
		}

		DocumentStyleLinkOperation _operation;
		private DocumentStyle _originalStyle;
		private DocumentStyle _replacementStyle;

		public DocumentStyleLinkOperation Operation{
			get => _operation;
			set => SetPropertyValue(nameof(Operation), ref _operation, value);
		}
	}

	public enum DocumentStyleLinkOperation{
		Replace,
		Ensure
	}
}