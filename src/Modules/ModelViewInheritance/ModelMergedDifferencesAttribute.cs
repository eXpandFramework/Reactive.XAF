using System;
using DevExpress.ExpressApp;

namespace Xpand.XAF.Modules.ModelViewInheritance {
    [AttributeUsage(AttributeTargets.Class,AllowMultiple = true)]
    public class ModelMergedDifferencesAttribute : Attribute {
        public Type TargetType { get; }
        public ViewType ViewType { get; }

        public ModelMergedDifferencesAttribute(string targetView, string sourceView) {
            TargetView = targetView;
            SourceView = sourceView;
        }

        public ModelMergedDifferencesAttribute(Type targetType,ViewType viewType=ViewType.DetailView) {
            TargetType = targetType;
            ViewType = viewType;
        }
        public string TargetView { get; }
        public string SourceView { get; }
    }
}