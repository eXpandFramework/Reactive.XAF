using System;

namespace Xpand.XAF.Modules.ModelMapper{
    [AttributeUsage(AttributeTargets.Property)]
    public class ModelMapperAttribute:Attribute{
        public ModelAdapterAlias Mapper { get; }

        public ModelMapperAttribute( ModelAdapterAlias mapper){
            Mapper = mapper;
        }
    }

    public enum ModelAdapterAlias{

        ASPxDateEditControl,
        DashboardViewEditor,
        DashboardViewer,
        HtmlEditor,
        ASPxHyperLinkControl,
        ASPxLookupDropDownEditControl,
        ASPxLookupFindEditControl,
        ASPxSearchDropDownEditControl,
        ASPxSpinEditControl,
        RepositoryItemBaseSpinEdit,
        RepositoryItemBlobBaseEdit,
        RepositoryItemButtonEdit,
        RepositoryItemCalcEdit,
        RepositoryItemCheckedComboBoxEdit,
        RepositoryItemCheckEdit,
        RepositoryItemColorEdit,
        RepositoryItemColorPickEdit,
        RepositoryItemColorComboBox,
        RepositoryItemDateEdit,
        RepositoryItemFontEdit,
        RepositoryItemHyperLinkEdit,
        RepositoryItemImageComboBox,
        RepositoryItemImageEdit,
        RepositoryItemLookUpEdit,
        RepositoryItemLookUpEditBase,
        RepositoryItemMarqueeProgressBar,
        RepositoryItemMemoEdit,
        RepositoryItemMemoExEdit,
        RepositoryItemMRUEdit,
        RepositoryItemObjectEdit,
        RepositoryItemPictureEdit,
        RepositoryItemPopupBase,
        RepositoryItemPopupBaseAutoSearchEdit,
        RepositoryItemPopupContainerEdit,
        RepositoryItemPopupCriteriaEdit,
        RepositoryItemPopupExpressionEdit,
        RepositoryItemPopupProgressEdit,
        RepositoryItemProtectedContextTextEdit,
        RepositoryItemRadioGroup,
        RepositoryItemRangeTrackBar,
        RepositoryItemRtfEditEx,
        RepositoryItemSpinEdit,
        RepositoryItemTextEdit,
        RepositoryItemTimeEdit,
        RepositoryItemTrackBar,
        RepositoryItemZoomTrackBar,
        RichEdit,
        LabelControl,
        FilterControl,
        LayoutControlGroup,
        UploadControl

    }

}