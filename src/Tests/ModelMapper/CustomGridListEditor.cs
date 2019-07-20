using System;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.BandedGrid;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Layout;
using Fasterflect;
using Xpand.XAF.Modules.ModelMapper.Tests.BOModel;

namespace Xpand.XAF.Modules.ModelMapper.Tests{
    class XafLayoutView:LayoutView,IModelSynchronizersHolder{
        public IModelSynchronizer GetSynchronizer(Component component){
            throw new NotImplementedException();
        }

        public void RegisterSynchronizer(Component component, IModelSynchronizer modelSynchronizer){
            throw new NotImplementedException();
        }

        public void RemoveSynchronizer(Component component){
            throw new NotImplementedException();
        }

        public void AssignSynchronizers(ColumnView columnView){
            throw new NotImplementedException();
        }

        protected override int CalculatePreferredWidth(LayoutViewField field, GridColumn column){
            return 0;
        }

        protected override void OnColumnsCollectionChanged(object sender, CollectionChangeEventArgs e){
            
        }

        public event EventHandler<CustomModelSynchronizerEventArgs> CustomModelSynchronizer;
    }
    class XafAdvBandedGridView:AdvBandedGridView,IModelSynchronizersHolder{
        public IModelSynchronizer GetSynchronizer(Component component){
            throw new NotImplementedException();
        }

        public void RegisterSynchronizer(Component component, IModelSynchronizer modelSynchronizer){
            throw new NotImplementedException();
        }

        public void RemoveSynchronizer(Component component){
            throw new NotImplementedException();
        }

        public void AssignSynchronizers(ColumnView columnView){
            throw new NotImplementedException();
        }


        protected override void OnColumnsCollectionChanged(object sender, CollectionChangeEventArgs e){
            
        }

        public event EventHandler<CustomModelSynchronizerEventArgs> CustomModelSynchronizer;
    }
    class CustomGridListEditor:GridListEditor,ISupportFooter{
        private readonly Type _viewType;
        private readonly Type _columnType;
        private readonly RepositoryItem _repositoryItem;

        public CustomGridListEditor(IModelListView model, Type viewType, Type columnType,
            RepositoryItem repositoryItem=null) : base(model){
            _viewType = viewType;
            _columnType = columnType;
            _repositoryItem = repositoryItem;
        }

        public override void ApplyModel(){
            
        }

        protected override void SubscribeGridViewEvents(){
            
        }

        protected override void SetupGridView(){
            
        }

        protected override void SetGridViewOptions(){
            
        }

        protected override void ApplyHtmlFormatting(bool htmlFormattingEnabled){
            
        }

        protected override void SubscribeToGridEvents(){
            
        }

        protected override ColumnView CreateGridViewCore(){
            var columnView = (ColumnView) _viewType.CreateInstance();
            var columns = new []{nameof(MM.Oid),nameof(MM.Test)}.Select(s => {
                var column = (GridColumn) _columnType.CreateInstance();
                column.Name = s;
                column.FieldName = s;
                column.ColumnEdit=_repositoryItem;
                return column;
            }).ToArray();
            
            columnView.Columns.AddRange(columns);
            return columnView;
        }

        public bool IsFooterVisible{ get; set; }
    }
}