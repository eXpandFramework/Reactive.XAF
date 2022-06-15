using System.ComponentModel;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;


namespace Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects{
	[DomainComponent]
	public class TemplateDocument:INotifyPropertyChanged,IObjectSpaceLink{
		private string _name;
		private object _key;
		private ApplyTemplateStyle _applyStyleTemplate;

		
		public string Name{
			get => _name;
			set{
				if (value == _name) return;
				_name = value;
				OnPropertyChanged();
			}
		}

		[Browsable(false)]
		public object Key{
			get => _key;
			set{
				if (Equals(value, _key)) return;
				_key = value;
				OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		[Browsable(false)]
		public IObjectSpace ObjectSpace{ get; set; }

		[Browsable(false)]
		public ApplyTemplateStyle ApplyStyleTemplate{
			get => _applyStyleTemplate;
			set{
				if (Equals(value, _applyStyleTemplate)) return;
				_applyStyleTemplate = value;
				OnPropertyChanged();
			}
		}


		
		
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null){
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}