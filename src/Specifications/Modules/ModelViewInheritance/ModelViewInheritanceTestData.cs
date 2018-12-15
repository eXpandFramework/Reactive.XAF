using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;

namespace DevExpress.XAF.Agnostic.Specifications.Modules.ModelViewInheritance{
    public class ModelViewInheritanceTestData : IEnumerable<object[]>{
        public IEnumerator<object[]> GetEnumerator(){
            var items = new[]{ViewType.ListView, ViewType.DetailView}
                .SelectMany(viewType => new[]{true, false}
                    .Select(b => new object[]{viewType,b}));
            foreach (var item in items){
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}