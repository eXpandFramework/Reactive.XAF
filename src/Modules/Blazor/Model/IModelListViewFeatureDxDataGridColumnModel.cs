using DevExpress.Blazor;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.XAF.ModelExtensions.Shapes;

namespace Xpand.XAF.Modules.Blazor.Model {
    [ModelDisplayName("GridListEditor Column Model")]
    public interface IModelListViewFeatureDxDataGridColumnModel : IModelListViewFeature,IDxDataGridColumnModel {
        IModelListViewColumns ListViewColumns { get; }
    }

    public interface IDxDataGridColumnModel {
        bool? AllowGroup { get; set; }
        bool? AllowSort { get; set; }
        bool? ExportEnabled { get; set; }
        int? ExportWidth { get; set; }
        GridColumnFilterMode? FilterMode { get; set; }
        bool? FilterRowEditorVisible { get; set; }
        GridFilterRowOperatorType? FilterRowOperatorType { get; set; }
        GridColumnSortMode? SortMode { get; set; }
        GridColumnSortOrder? SortOrder { get; set; }
        GridTextAlignment? TextAlignment { get; set; }
        string UnboundExpression { get; set; }
        GridUnboundColumnType? UnboundType { get; set; }
        bool? Visible { get; set; }
        int? VisibleIndex { get; set; }
        string Width { get; set; }
        int? MinWidth { get; set; }
    }


}