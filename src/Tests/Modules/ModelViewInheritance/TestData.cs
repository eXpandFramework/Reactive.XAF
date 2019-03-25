using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;

namespace Xpand.XAF.Agnostic.Tests.Modules.ModelViewInheritance{
    public class ModelViewInheritanceTestData : IEnumerable<object[]>{
        public IEnumerator<object[]> GetEnumerator(){
            var items = new[]{ViewType.ListView, ViewType.DetailView}
                .SelectMany(viewType => new[]{true, false}
                    .Select(b => new object[]{viewType,b}));
//            yield return new object[]{ViewType.ListView,false};
            foreach (var item in items){
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}