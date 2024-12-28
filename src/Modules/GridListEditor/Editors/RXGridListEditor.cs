using DevExpress.Data;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Registrator;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Base.ViewInfo;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;

namespace Xpand.XAF.Modules.GridListEditor.Editors {
    [ListEditor(typeof(object), false)]
    public class RXGridListEditor(IModelListView model) : DevExpress.ExpressApp.Win.Editors.GridListEditor(model) {
        // protected override GridControl CreateGridControl() => new RXGridControl();
        // protected override ColumnView CreateGridViewCore() => new RXGridView();
    }
    
    public class RXGridViewInfoRegistrator : GridInfoRegistrator {
        // public override string ViewName => nameof(RXGridView);
        // public override BaseView CreateView(GridControl grid) => new RXGridView(){GridControl = grid};
        // public override BaseViewInfo CreateViewInfo(BaseView view) => new RXGridViewInfo((GridView)view );
    }
    public class RXDataController(GridView view) : CurrencyDataController {
        protected override void BuildVisibleIndexes() {

            base.BuildVisibleIndexes();
            if (GroupedColumnCount == 0) return;
            int[] indexes = new int[VisibleIndexes.Count];
            VisibleIndexes.CopyTo(indexes, 0);
            VisibleIndexes.Clear();
            foreach (int rowHandle in indexes) {
                if (IsGroupRowHandle(rowHandle) && view.GetChildRowCount(rowHandle) < 2) continue;
                VisibleIndexes.Add(rowHandle);
            }
        }
    }
    public class RXGridViewInfo(GridView view) : GridViewInfo(view) {
        public override int GetRowFooterCount(int rowHandle, int nextRowHandle, bool isExpanded) {
            GroupRowInfo rowInfo = View.DataController.GroupInfo.GetGroupRowInfoByControllerRowHandle(rowHandle);
            GroupRowInfo newRowInfo = View.DataController.GroupInfo.GetGroupRowInfoByControllerRowHandle(nextRowHandle);
            newRowInfo = GetStartGroup(newRowInfo, nextRowHandle);
            if (newRowInfo == null || newRowInfo == rowInfo) return base.GetRowFooterCount(rowHandle, nextRowHandle, isExpanded);
            return base.GetRowFooterCount(rowHandle, newRowInfo.Handle, isExpanded);
        }
        GroupRowInfo GetStartGroup(GroupRowInfo newRowInfo, int nextRowHandle) {
            if (newRowInfo == null) return null;
            while (newRowInfo.ParentGroup != null) {
                if (newRowInfo.ParentGroup.ChildControllerRow == nextRowHandle)
                    newRowInfo = newRowInfo.ParentGroup;
                else
                    break;
            }
            return newRowInfo;
        }
    }
    
    public class RXGridView : XafGridView {
        public RXGridView()
             {
            OptionsBehavior.AutoExpandAllGroups = true;
            OptionsView.ShowGroupedColumns = true;
        }
        
        protected override string ViewName => nameof(RXGridView);
        protected override BaseGridController CreateDataController() => new RXDataController(this);
        // protected override bool GetShowGroupedColumns() {
        //     return AllowPartialGroups || OptionsView.ShowGroupedColumns;
        // }
    }
    public class RXGridControl : GridControl {
        protected override void RegisterAvailableViewsCore(InfoCollection collection) 
            => collection.Add(new RXGridViewInfoRegistrator());

        protected override BaseView CreateDefaultView() => CreateView(nameof(RXGridView));
    }
}