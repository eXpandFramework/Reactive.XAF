using DevExpress.Blazor;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.XAF.ModelExtensions.Shapes;

namespace Xpand.XAF.Modules.Blazor.Model {
    [ModelDisplayName("GridListEditor Column Model")]
    public interface IModelListViewFeatureDxDataGridColumnModel : IModelListViewFeature,IDxDataGridColumnModel {
        IModelListViewColumns ListViewColumns { get; }
    }

    public interface IDxDataGridColumnModel {
        DataGridTextAlign TextAlignment { get; set; }
        bool? AllowSort { get; set; }
        DataGridColumnSortOrder SortOrder { get; set; }
        int? SortIndex { get; set; }
        int? GroupIndex { get; set; }
        bool? AllowGroup { get; set; }
        bool? AllowFilter { get; set; }
        DataEditorClearButtonDisplayMode? ClearButtonDisplayMode { get; set; }
        bool? EditorVisible { get; set; }
        int? EditFormVisibleIndex { get; set; }
        DataGridFixedStyle? FixedStyle { get; set; }
        bool? ShowInColumnChooser { get; set; }
    }


}