using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Xpand.Source.Extensions.XAF.XafApplication;

namespace Xpand.XAF.Modules.ModelViewInheritance.Tests{
    public class ModelViewInheritanceTestData : IEnumerable<object[]>{
        public IEnumerator<object[]> GetEnumerator(){
            var items = new[]{ViewType.ListView, ViewType.DetailView}
                .SelectMany(viewType => new[]{true, false}
                    .Select(b => (viewType,attribute:b))
                    .SelectMany(_ => new[]{Platform.Win,Platform.Web}
                        .Select(platform => new object[]{_.viewType,_.attribute,platform})));
//            yield return new object[]{ViewType.ListView,true,Platform.Web};
//            yield return new object[]{ViewType.DetailView,false,Platform.Win};
            foreach (var item in items){
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}