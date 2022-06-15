using DevExpress.ExpressApp;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;

using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects{
	
	public class DocumentStyleLinkTemplate:CustomBaseObject{
		public DocumentStyleLinkTemplate(Session session) : base(session){
		}
		
		string _name;
		[Size(255)][RuleRequiredField]
		public string Name{
			get => _name;
			set => SetPropertyValue(nameof(Name), ref _name, value);
		}

		bool _keepUnused;

		public bool KeepUnused{
			get => _keepUnused;
			set => SetPropertyValue(nameof(KeepUnused), ref _keepUnused, value);
		}

		[Association("DocumentStyleLinkTemplate-DocumentStyleLinks")][Aggregated]
		[CollectionOperationSet(AllowAdd = false)]
		public XPCollection<DocumentStyleLink> DocumentStyleLinks => GetCollection<DocumentStyleLink>(nameof(DocumentStyleLinks));
	}
}