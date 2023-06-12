using DevExpress.Blazor;
using DevExpress.ExpressApp.Model;

using Xpand.Extensions.XAF.ModelExtensions.Shapes;

namespace Xpand.XAF.Modules.Blazor.Model {
    [ModelDisplayName("GridListEditor Grid Model")]
    public interface IModelListViewFeatureDxDataGridModel:IModelListViewFeature {
        bool? AllowGroup { get; set; }
        bool? AllowSelectRowByClick { get; set; }
        bool? AllowSort { get; set; }
        bool? AutoCollapseDetailRow { get; set; }
        bool? AutoExpandAllGroupRows { get; set; }
        GridColumnResizeMode ColumnResizeMode { get; set; }
        string CssClass { get; set; }
        GridEditNewRowPosition? EditNewRowPosition { get; set; }
        GridEditorRenderMode? EditorRenderMode { get; set; }
        bool? FocusRowEnabled { get; set; }
        GridFooterDisplayMode? FooterDisplayMode { get; set; }
        GridGroupFooterDisplayMode? GroupFooterDisplayMode { get; set; }
        int? PageIndex { get; set; }
        int? PageSize { get; set; }
        bool? PageSizeSelectorAllRowsVisible { get; set; }
        bool? PageSizeSelectorVisible { get; set; }
        bool? PagerAutoHideNavButtons { get; set; }
        PagerNavigationMode? PagerNavigationMode { get; set; }
        GridPagerPosition? PagerPosition { get; set; }
        int? PagerSwitchToInputBoxButtonCount { get; set; }
        bool? PagerVisible { get; set; }
        int? PagerVisibleNumericButtonsCount { get; set; }
        string PopupEditFormCssClass { get; set; }
        int? SearchBoxInputDelay { get; set; }
        string SearchBoxNullText { get; set; }
        string SearchText { get; set; }
        GridSelectAllCheckboxMode? SelectAllCheckboxMode { get; set; }
        GridSelectionMode? SelectionMode { get; set; }
        bool? ShowAllRows { get; set; }
        bool? ShowFilterRow { get; set; }
        bool? ShowGroupPanel { get; set; }
        bool? ShowGroupedColumns { get; set; }
        bool? ShowSearchBox { get; set; }
        SizeMode? SizeMode { get; set; }
        bool? ValidationEnabled { get; set; }
        IModelListViewKeys ListViews { get; }
        // bool?? ShowColumnHeaders { get; set; }
        // bool?? ShowFilterRow { get; set; }
        // bool?? ShowPager { get; set; }
        // bool?? PagerPageSizeSelectorVisible { get; set; }
        // bool?? PagerAllDataRowsItemVisible { get; set; }
        // int? PageSize { get; set; }
        // int ?PageIndex { get; set; }
        // int PageCount { get; set; }
        // PagerNavigationMode PagerNavigationMode { get; set; }
        // int? PagerSwitchToInputBoxButtonCount { get; set; }
        // bool?? PagerAutoHideNavButtons { get; set; }
        // int? PagerVisibleNumericButtonCount { get; set; }
        // DataGridColumnResizeMode? ColumnResizeMode { get; set; }
        // DataGridSelectAllMode? SelectAllMode { get; set; }
        // DataGridSelectionMode? SelectionMode { get; set; }
        // DataGridEditMode? EditMode { get; set; }
        //  string? PopupEditFormHeaderText { get; set; }
        //  string? KeyFieldName { get; set; }
        //  string? DataRowCssClass { get; set; }
        // DataGridNavigationMode? DataNavigationMode { get; set; }
        // ScrollBarMode? VerticalScrollBarMode { get; set; }
        // ScrollBarMode? HorizontalScrollBarMode { get; set; }
        // int? VerticalScrollableHeight { get; set; }
        // bool?? AllowSort { get; set; }
        // bool?? AllowColumnDragDrop { get; set; }
        // bool?? ShowGroupPanel { get; set; }
        // bool?? ShowDetailRow { get; set; }
        // bool?? AutoCollapseDetailRow { get; set; }
        // bool?? ShowGroupedColumns { get; set; }
        // SizeMode? InnerComponentSizeMode { get; set; }
        // string? CssClass { get; set; }
    }
}