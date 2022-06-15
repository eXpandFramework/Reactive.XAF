using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp.DC;


namespace Xpand.XAF.Modules.Reactive.Tests.BOModel{
    [DomainComponent]
    public class NonPersistentObject:INotifyPropertyChanged{
        private R _r;
        public List<NonPersistentObject> Childs{ get; }=new List<NonPersistentObject>();


        public event PropertyChangedEventHandler PropertyChanged;

        public R R{
            get => _r;
            set{
                if (Equals(value, _r)) return;
                _r = value;
                OnPropertyChanged();
            }
        }

        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null){
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}