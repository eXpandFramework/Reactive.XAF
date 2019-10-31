using System;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.Utils.Reflection;
using Fasterflect;
using HarmonyLib;
using Xpand.Extensions.XAF.Model;

namespace Xpand.XAF.Modules.CloneModelView{
    [HarmonyPatch(MethodType.Normal, typeof(Attribute))]
    [HarmonyPatch(typeof(ModelApplicationCodeGenerator))]
    [HarmonyPatch("GetAttribute")]
    public class MyClass{
        static void Postfix(Type baseInterface){
            //...
        }

        [HarmonyTargetMethod]
// NOTE: not passing harmony instance with attributes is broken in 1.2.0.1
        static MethodBase CalculateMethod(){
            return typeof(ModelApplicationCodeGenerator)
                .Methods(new[]{typeof(Type)}, Flags.Static | Flags.NonPublic, "GetAttribute").First();
        }
    }
    public class ModelViewClonerUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator> {
        
        private static bool GetAttribute<T>(ref ModelNodesGeneratorAttribute __result,Type baseInterface){
            if (baseInterface == typeof(IModelViews)){
                return false;
            }

            return true;
        }
        static ModelViewClonerUpdater(){
            var harmony = new Harmony(typeof(IModelViewController).Namespace);
            harmony.PatchAll();
//            var original = typeof(ModelApplicationCodeGenerator).Methods(new []{typeof(Type)}, Flags.Static|Flags.NonPublic,"GetAttribute").First();
//            var patched = typeof(ModelViewClonerUpdater).GetMethod("GetAttribute",BindingFlags.Static|BindingFlags.NonPublic);
//
//            harmony.Patch(original, postfix:new HarmonyMethod(patched));
        }
        public override void UpdateNode(ModelNode node) {
            var modelClasses = node.Application.BOModel.Where(modelClass => modelClass.TypeInfo.FindAttribute<CloneModelViewAttribute>() != null);
            
            var master = node.Application.Master();
            var cloneHomeLayer = master.NewModelApplication(nameof(ModelViewClonerUpdater));
            foreach (var modelClass in modelClasses) {
                var cloneViewAttributes = modelClass.TypeInfo.FindAttributes<CloneModelViewAttribute>(false).OrderBy(viewAttribute => viewAttribute.ViewType);
                foreach (var cloneViewAttribute in cloneViewAttributes) {
                    if (node.Application.Views[cloneViewAttribute.ViewId]==null) {
                        var tuple = GetModelView(modelClass, cloneViewAttribute);
                        var cloneNodeFrom = ((ModelNode)tuple.objectView).Clone(cloneViewAttribute.ViewId);
                        AssignAsDefaultView(cloneViewAttribute, (IModelObjectView) cloneNodeFrom,tuple.isLookup);
                        if (tuple.objectView is IModelListView && !(string.IsNullOrEmpty(cloneViewAttribute.DetailView))) {
                            var modelDetailView =node.Application.Views.OfType<IModelDetailView>().FirstOrDefault(view 
                                => view.Id == cloneViewAttribute.DetailView);
                            if (modelDetailView == null)
                                throw new NullReferenceException(cloneViewAttribute.DetailView);
                            ((IModelListView) cloneNodeFrom).DetailView = modelDetailView;
                        }
                    }
                }
            }
            master.InsertLayer(cloneHomeLayer);
        }

        public static Type ModelViewType( CloneViewType viewType){
            if (viewType == CloneViewType.ListView||viewType == CloneViewType.LookupListView) return typeof(IModelListView);
            return typeof(IModelDetailView);
        }

        void AssignAsDefaultView(CloneModelViewAttribute cloneModelViewAttribute, IModelObjectView modelView,bool isLookup) {
            if (cloneModelViewAttribute.IsDefault) {
                if (modelView is IModelListView view) {
                    if (!isLookup){
                        view.ModelClass.DefaultListView = view;
                    }
                    else{
                        view.ModelClass.DefaultLookupListView = view;
                    }
                }
                else {
                    modelView.ModelClass.DefaultDetailView = (IModelDetailView) modelView;
                }
            }
        }

        (IModelObjectView objectView,bool isLookup) GetModelView(IModelClass modelClass, CloneModelViewAttribute cloneModelViewAttribute) {
            if (cloneModelViewAttribute.ViewType == CloneViewType.LookupListView)
                return (modelClass.DefaultLookupListView,true);
            if (cloneModelViewAttribute.ViewType == CloneViewType.DetailView)
                return (modelClass.DefaultDetailView,false);
            return (modelClass.DefaultListView,false);
        }
    }
}