using DevExpress.Blazor;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;

namespace Xpand.XAF.Modules.Blazor.Model {
    
    public interface IModelDxDataGridModel:IModelNode {
        bool? ShowColumnHeaders { get; set; }
        bool? ShowFilterRow { get; set; }
        bool? ShowPager { get; set; }
        bool? PagerPageSizeSelectorVisible { get; set; }
        bool? PagerAllDataRowsItemVisible { get; set; }
        int? PageSize { get; set; }
        int ?PageIndex { get; set; }
        int PageCount { get; set; }
        PagerNavigationMode PagerNavigationMode { get; set; }
        int? PagerSwitchToInputBoxButtonCount { get; set; }
        bool? PagerAutoHideNavButtons { get; set; }
        int? PagerVisibleNumericButtonCount { get; set; }
        DataGridColumnResizeMode? ColumnResizeMode { get; set; }
        DataGridSelectAllMode? SelectAllMode { get; set; }
        DataGridSelectionMode? SelectionMode { get; set; }
        DataGridEditMode? EditMode { get; set; }
        [CanBeNull] string PopupEditFormHeaderText { get; set; }
        [CanBeNull] string KeyFieldName { get; set; }
        [CanBeNull] string DataRowCssClass { get; set; }
        DataGridNavigationMode? DataNavigationMode { get; set; }
        ScrollBarMode? VerticalScrollBarMode { get; set; }
        ScrollBarMode? HorizontalScrollBarMode { get; set; }
        int? VerticalScrollableHeight { get; set; }
        bool? AllowSort { get; set; }
        bool? AllowColumnDragDrop { get; set; }
        bool? ShowGroupPanel { get; set; }
        bool? ShowDetailRow { get; set; }
        bool? AutoCollapseDetailRow { get; set; }
        bool? ShowGroupedColumns { get; set; }
        SizeMode? InnerComponentSizeMode { get; set; }
        string CssClass { get; set; }
    }
}