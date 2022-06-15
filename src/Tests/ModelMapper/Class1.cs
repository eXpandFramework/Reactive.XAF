[assembly:Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.ModelMapperServiceAttribute("a1ef292e-685a-a661-0d8f-1d6220eb1f5c")]

[assembly:Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.ModelMapperTypeAttribute("DevExpress.XtraRichEdit.RichEditControl","DevExpress.XtraRichEdit.v21.2","46e007a0-e595-4a33-86ad-8f7793ac9d16","a6de2d5e-0acf-e784-42ec-b1d7a05f315e")][assembly:Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.ModelMapperServiceAttribute("a1ef292e-685a-a661-0d8f-1d6220eb1f5c")]

[assembly:Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.ModelMapperTypeAttribute("Xpand.XAF.Modules.ModelMapper.Services.Predefined.PropertyEditorControlMap","Xpand.XAF.Modules.ModelMapper","f6ad341e-4918-4c21-82ec-01089912f22b","827a7b8e-b57d-deb3-c118-a16e0ec8f593")]
[assembly:System.Reflection.AssemblyVersionAttribute("4.212.9.8")]
[assembly:System.Reflection.AssemblyFileVersionAttribute("4.212.9.8")]
[DevExpress.ExpressApp.DC.DomainLogicAttribute(typeof(IModelPropertyEditorControlMapMapModelMappers))]
public class IModelPropertyEditorControlMapMapModelMappersDomainLogic{public static int? Get_Index(IModelPropertyEditorControlMapMapModelMappers mapper){return 0;}}
[DevExpress.ExpressApp.Model.ModelDisplayNameAttribute("PropertyEditorControlMap")]

[DevExpress.ExpressApp.Model.ModelAbstractClassAttribute()]
public interface IModelXpandXAFModulesModelMapperServicesPredefined_PropertyEditorControlMap:Xpand.XAF.Modules.ModelMapper.IModelModelMap{


}
[Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.ModelMapLinkAttribute("Xpand.XAF.Modules.ModelMapper.Services.Predefined.PropertyEditorControlMap, Xpand.XAF.Modules.ModelMapper, Version=4.212.9.8, Culture=neutral, PublicKeyToken=c52ffed5d5ff0958")]

[DevExpress.ExpressApp.Model.ModelAbstractClassAttribute()]
public interface IModelPropertyEditorControlMapMap:Xpand.XAF.Modules.ModelMapper.IModelModelMapContainer{
IModelXpandXAFModulesModelMapperServicesPredefined_PropertyEditorControlMaps Controls{get;}

}
[System.ComponentModel.DescriptionAttribute("These mappers relate to Application.ModelMapper.MapperContexts and applied first.")]
[DevExpress.ExpressApp.Model.ModelNodesGeneratorAttribute(typeof(Xpand.XAF.Modules.ModelMapper.ModelMapperContextNodeGenerator))]
[DevExpress.Persistent.Base.ImageNameAttribute("Context_Menu_Show_In_Popup")]
public interface IModelPropertyEditorControlMapMapModelMappers:DevExpress.ExpressApp.Model.IModelList<Xpand.XAF.Modules.ModelMapper.IModelMapperContextContainer>,DevExpress.ExpressApp.Model.IModelNode{}

public interface IModelXpandXAFModulesModelMapperServicesPredefined_PropertyEditorControlMaps:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelXpandXAFModulesModelMapperServicesPredefined_PropertyEditorControlMap>{}
[DevExpress.ExpressApp.DC.DomainLogicAttribute(typeof(IModelRichEditControlMapModelMappers))]
public class IModelRichEditControlMapModelMappersDomainLogic{public static int? Get_Index(IModelRichEditControlMapModelMappers mapper){return 0;}}
[DevExpress.ExpressApp.Model.ModelDisplayNameAttribute("RichEditControl")]
[DevExpress.Persistent.Base.ImageNameAttribute("RichEditBookmark_16x16")]

[System.ComponentModel.DescriptionAttribute("A control to create, load, modify, print, save and convert rich text documents in" +
    " different formats.")]
[Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.ModelMapLinkAttribute("DevExpress.XtraRichEdit.RichEditControl, DevExpress.XtraRichEdit.v21.2, Version=2" +
    "1.2.8.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a")]
public interface IModelDevExpressXtraRichEdit_RichEditControl:Xpand.XAF.Modules.ModelMapper.IModelModelMap,IModelXpandXAFModulesModelMapperServicesPredefined_PropertyEditorControlMap,DevExpress.ExpressApp.Model.IModelNode{
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the overtype mode is enabled for the RichEdit control.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Behavior")]
System.Boolean? Overtype{get;set;}
[DevExpress.XtraEditors.DXCategoryAttribute("Layout")]
[System.ComponentModel.DescriptionAttribute("Gets or sets the type of the RichEditControl’s View.")]
DevExpress.XtraRichEdit.RichEditViewType? ActiveViewType{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the top visible position in the scrolled document.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Int64? VerticalScrollValue{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a measure unit used within the control.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Layout")]
DevExpress.Office.DocumentUnit? Unit{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether document modifications are prohibited.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Behavior")]
System.Boolean? ReadOnly{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the RichEditControl resizes to accommodate the displayed tex" +
    "t.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
DevExpress.XtraRichEdit.AutoSizeMode? AutoSizeMode{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the border style for the RichEdit control.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
[DevExpress.Utils.Serializing.XtraSerializableProperty(DevExpress.Utils.Serializing.XtraSerializationFlags.DefaultValue)]
DevExpress.XtraEditors.Controls.BorderStyles? BorderStyle{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to the variety of options which can be specified for the RichEdit" +
    "Control.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Options")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_RichEditControlOptions Options{get;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the tooltip controller component that controls the appearance, posit" +
    "ion and the content of the hints displayed by the RichEditControl.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
IModelDevExpressUtils_ToolTipController ToolTipController{get;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the drag-and-drop mode which is active in the RichEditControl.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Behavior")]
DevExpress.XtraRichEdit.DragDropMode? DragDropMode{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a value indicating whether pressing the TAB key types a TAB characte" +
    "r instead of moving the focus to the next control in the tab order.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Behavior")]
System.Boolean? AcceptsTab{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a value indicating whether pressing the RETURN key is processed by t" +
    "he RichEditControl.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Behavior")]
System.Boolean? AcceptsReturn{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a value indicating whether pressing the ESC key is processed by the " +
    "RichEditControl.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Behavior")]
System.Boolean? AcceptsEscape{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the caret is shown if a RichEditControl’s content is read-on" +
    "ly.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Behavior")]
System.Boolean? ShowCaretInReadOnly{get;set;}
[System.ComponentModel.DescriptionAttribute("Enables you to fire data binding events immediately for several text properties, " +
    "resolving issues with multiple RichEdit controls bound to the same data source.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Behavior")]
System.Boolean? UseDeferredDataBindingNotifications{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to the settings that specify the RichEdit control’s look and feel" +
    ".")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressLookAndFeel_UserLookAndFeel LookAndFeel{get;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to display tooltips for data fields in documents.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Boolean? EnableToolTips{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the control’s content as a plain text.")]
[System.ComponentModel.EditorAttribute("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=4.0.0." +
    "0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",typeof(System.Drawing.Design.UITypeEditor))]
System.String Text{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to the collection of hyphenation dictionaries.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Hyphenation")]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
[System.ComponentModel.EditorAttribute("DevExpress.XtraRichEdit.Design.HyphenationDictionaryItemCollectionEditor,DevExpre" +
    "ss.XtraRichEdit.v21.2.Design, Version=21.2.8.0, Culture=neutral, PublicKeyToken=" +
    "b88d1754d700e49a",typeof(System.Drawing.Design.UITypeEditor))]
IModelDevExpressXtraRichEditAPINative_IHyphenationDictionarys HyphenationDictionaries{get;}
[System.ComponentModel.DescriptionAttribute("Specifies the page order in Print Layout view and in Print Preview.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Layout")]
DevExpress.XtraRichEdit.DocumentViewDirection? DocumentViewDirection{get;set;}
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The description that will be reported to accessibility clients.")]
System.String AccessibleDescription{get;set;}
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The name that will be reported to accessibility clients.")]
System.String AccessibleName{get;set;}
[System.ComponentModel.DescriptionAttribute("The role that will be reported to accessibility clients.")]
System.Windows.Forms.AccessibleRole? AccessibleRole{get;set;}
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.Repaint)]
[System.ComponentModel.DescriptionAttribute("Defines the edges of the container to which a certain control is bound. When a co" +
    "ntrol is anchored to an edge, the distance between the control\'s closest edge an" +
    "d the specified edge will remain constant. ")]
System.Windows.Forms.AnchorStyles? Anchor{get;set;}
[System.ComponentModel.DescriptionAttribute("Indicates whether this component raises validation events. ")]
System.Boolean? CausesValidation{get;set;}
[System.ComponentModel.DescriptionAttribute("The shortcut menu to display when the user right-clicks the control.")]
IModelSystemWindowsForms_ContextMenuStrip ContextMenuStrip{get;}
[System.ComponentModel.AmbientValueAttribute(null)]
[System.ComponentModel.DescriptionAttribute("The cursor that appears when the pointer moves over the control.")]
IModelSystemWindowsForms_Cursor Cursor{get;}
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[System.ComponentModel.ParenthesizePropertyNameAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The data bindings for the control.")]
IModelSystemWindowsForms_Bindings DataBindings{get;}

IModelSystemDrawing_Font DefaultFont{get;}
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.Repaint)]
[System.ComponentModel.DescriptionAttribute("Defines which borders of the control are bound to the container. ")]
System.Windows.Forms.DockStyle? Dock{get;set;}
[System.Runtime.InteropServices.DispIdAttribute(-514)]
[System.ComponentModel.DescriptionAttribute("Indicates whether the control is enabled.")]
System.Boolean? Enabled{get;set;}
[System.ComponentModel.DescriptionAttribute("The coordinates of the upper-left corner of the control relative to the upper-lef" +
    "t corner of its container.")]
System.Drawing.Point? Location{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies space between this control and another control\'s margin.")]
System.Windows.Forms.Padding? Margin{get;set;}
[System.ComponentModel.AmbientValueAttribute(typeof(System.Drawing.Size),"0, 0")]
[System.ComponentModel.DescriptionAttribute("Specifies the maximum size of the control.")]
System.Drawing.Size? MaximumSize{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the minimum size of the control.")]
System.Drawing.Size? MinimumSize{get;set;}
[System.ComponentModel.DescriptionAttribute("The size of the control in pixels.")]
System.Drawing.Size? Size{get;set;}
[System.ComponentModel.MergablePropertyAttribute(false)]
[System.ComponentModel.DescriptionAttribute("Determines the index in the TAB order that this control will occupy.")]
System.Int32? TabIndex{get;set;}
[System.Runtime.InteropServices.DispIdAttribute(-516)]
[System.ComponentModel.DescriptionAttribute("Indicates whether the user can use the TAB key to give focus to the control.")]
System.Boolean? TabStop{get;set;}
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
[System.ComponentModel.BrowsableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("When this property is true, the Cursor property of the control and its child cont" +
    "rols is set to WaitCursor.")]
System.Boolean? UseWaitCursor{get;set;}
[System.ComponentModel.DescriptionAttribute("Determines whether the control is visible or hidden.")]
System.Boolean? Visible{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the interior spacing of a control.")]
System.Windows.Forms.Padding? Padding{get;set;}
[System.ComponentModel.AmbientValueAttribute(System.Windows.Forms.ImeMode.Inherit)]
[System.ComponentModel.DescriptionAttribute("Determines the IME (Input Method Editor) status of the object when selected.")]
System.Windows.Forms.ImeMode? ImeMode{get;set;}

}
[System.ComponentModel.DescriptionAttribute("These mappers relate to Application.ModelMapper.MapperContexts and applied first.")]
[DevExpress.ExpressApp.Model.ModelNodesGeneratorAttribute(typeof(Xpand.XAF.Modules.ModelMapper.ModelMapperContextNodeGenerator))]
[DevExpress.Persistent.Base.ImageNameAttribute("Context_Menu_Show_In_Popup")]
public interface IModelRichEditControlMapModelMappers:DevExpress.ExpressApp.Model.IModelList<Xpand.XAF.Modules.ModelMapper.IModelMapperContextContainer>,DevExpress.ExpressApp.Model.IModelNode{}


public interface IModelDevExpressLookAndFeel_BaseLookAndFeelPainters:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelDevExpressUtilsDrawing_GridGroupPanelPainter GroupPanel{get;}

IModelDevExpressUtilsDrawing_ProgressBarObjectPainter ProgressBar{get;}

IModelDevExpressUtilsDrawing_ProgressBarObjectPainter MarqueeProgressBar{get;}

IModelDevExpressUtilsDrawing_SizeGripObjectPainter SizeGrip{get;}

IModelDevExpressUtilsDrawing_ObjectPainter Button{get;}

IModelDevExpressUtilsDrawing_BorderPainter Border{get;}

IModelDevExpressUtilsDrawing_HeaderObjectPainter Header{get;}

IModelDevExpressUtilsDrawing_ObjectPainter SortedShape{get;}

IModelDevExpressUtilsDrawing_ObjectPainter OpenCloseButton{get;}

IModelDevExpressUtilsDrawing_FooterPanelPainter FooterPanel{get;}

IModelDevExpressUtilsDrawing_FooterCellPainter FooterCell{get;}

IModelDevExpressUtilsDrawing_IndicatorObjectPainter Indicator{get;}

}

[System.ComponentModel.TypeConverterAttribute(typeof(DevExpress.LookAndFeel.UserLookAndFeelConverter))]
[System.ComponentModel.EditorAttribute("DevExpress.Utils.Design.UserLookAndFeelUITypeEditor, DevExpress.Design.v21.2, Ver" +
    "sion=21.2.8.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a",typeof(System.Drawing.Design.UITypeEditor))]
public interface IModelDevExpressLookAndFeel_UserLookAndFeel:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets a custom hue applied to some skin elements.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Drawing.Color? SkinMaskColor{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the second custom hue, typically applied to some skin elements when " +
    "they are highlighted/hovered.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Drawing.Color? SkinMaskColor2{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the name of a skin style.")]
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.String SkinName{get;set;}
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the current object’s settings are in effect.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Boolean? UseDefaultLookAndFeel{get;set;}

}


public interface IModelDevExpressUtils_AppearanceDefault:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

DevExpress.Utils.DefaultBoolean? HighPriority{get;set;}

System.Int32? FontSizeDelta{get;set;}

System.Drawing.FontStyle? FontStyleDelta{get;set;}

DevExpress.Utils.HorzAlignment? HAlignment{get;set;}

DevExpress.Utils.VertAlignment? VAlignment{get;set;}

System.Drawing.Color? ForeColor{get;set;}

System.Drawing.Color? BackColor{get;set;}

System.Drawing.Color? BackColor2{get;set;}

System.Drawing.Color? BorderColor{get;set;}

IModelSystemDrawing_Font Font{get;}

System.Drawing.Drawing2D.LinearGradientMode? GradientMode{get;set;}

}

[System.ComponentModel.TypeConverterAttribute(typeof(DevExpress.Utils.AppearanceObjectConverter))]
[System.ComponentModel.EditorAttribute("DevExpress.Utils.Design.AppearanceObjectUITypeEditor, DevExpress.Design.v21.2, Ve" +
    "rsion=21.2.8.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a",typeof(System.Drawing.Design.UITypeEditor))]
public interface IModelDevExpressUtils_AppearanceObject:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Provides access to the appearance object’s options.")]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
[DevExpress.Utils.Serializing.XtraSerializableProperty(DevExpress.Utils.Serializing.XtraSerializationVisibility.Content,DevExpress.Utils.Serializing.XtraSerializationFlags.DefaultValue)]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.AppearanceObject.Options")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
IModelDevExpressUtils_AppearanceOptions Options{get;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the foreground color.")]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.AppearanceObject.ForeColor")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
[System.ComponentModel.EditorAttribute("DevExpress.Utils.Design.DXForeSkinColorEditor, DevExpress.Design.v21.2, Version=2" +
    "1.2.8.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a",typeof(System.Drawing.Design.UITypeEditor))]
[System.ComponentModel.TypeConverterAttribute(typeof(DevExpress.Utils.Colors.DXSkinColorConverter))]
System.Drawing.Color? ForeColor{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the border color.")]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.AppearanceObject.BorderColor")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
[System.ComponentModel.EditorAttribute("DevExpress.Utils.Design.DXBorderSkinColorEditor, DevExpress.Design.v21.2, Version" +
    "=21.2.8.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a",typeof(System.Drawing.Design.UITypeEditor))]
[System.ComponentModel.TypeConverterAttribute(typeof(DevExpress.Utils.Colors.DXSkinColorConverter))]
System.Drawing.Color? BorderColor{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the background color if the AppearanceObject.BackColor2 property’s v" +
    "alue is Color.Empty. Otherwise, it specifies the gradient’s starting color.")]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.AppearanceObject.BackColor")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
[System.ComponentModel.EditorAttribute("DevExpress.Utils.Design.DXBackSkinColorEditor, DevExpress.Design.v21.2, Version=2" +
    "1.2.8.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a",typeof(System.Drawing.Design.UITypeEditor))]
[System.ComponentModel.TypeConverterAttribute(typeof(DevExpress.Utils.Colors.DXSkinColorConverter))]
System.Drawing.Color? BackColor{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the end color of the  background’s gradient brush.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.AppearanceObject.BackColor2")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Drawing.Color? BackColor2{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to text rendering options (horizontal and vertical alignment, wor" +
    "d wrapping, trimming options, etc.).")]
[System.ComponentModel.BrowsableAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
[DevExpress.Utils.Serializing.XtraSerializableProperty(DevExpress.Utils.Serializing.XtraSerializationVisibility.Content,DevExpress.Utils.Serializing.XtraSerializationFlags.DefaultValue)]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.AppearanceObject.TextOptions")]
[DevExpress.XtraEditors.DXCategoryAttribute("Font")]
IModelDevExpressUtils_TextOptions TextOptions{get;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the font used to paint the text.")]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.AppearanceObject.Font")]
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[DevExpress.XtraEditors.DXCategoryAttribute("Font")]
IModelSystemDrawing_Font Font{get;}
[System.ComponentModel.DescriptionAttribute("Gets or sets an integer value by which the font size is adjusted.")]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.AppearanceObject.FontSizeDelta")]
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]

[DevExpress.XtraEditors.DXCategoryAttribute("Font")]
System.Int32? FontSizeDelta{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets an additional style to be applied to the font.")]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.AppearanceObject.FontStyleDelta")]
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[DevExpress.Utils.Serializing.XtraSerializableProperty(99)]
[DevExpress.XtraEditors.DXCategoryAttribute("Font")]
System.Drawing.FontStyle? FontStyleDelta{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the background gradient’s direction.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.AppearanceObject.GradientMode")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Drawing.Drawing2D.LinearGradientMode? GradientMode{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the default font for controls.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
IModelSystemDrawing_Font DefaultFont{get;}
[System.ComponentModel.DescriptionAttribute("Gets and sets the font used to display text on menus.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
IModelSystemDrawing_Font DefaultMenuFont{get;}

}


public interface IModelDevExpressUtils_AppearanceOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to use the AppearanceObject.ForeColor property value.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.AppearanceOptions.UseForeColor")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Boolean? UseForeColor{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to use the AppearanceObject.TextOptions property value.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.AppearanceOptions.UseTextOptions")]
[DevExpress.XtraEditors.DXCategoryAttribute("Font")]
System.Boolean? UseTextOptions{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to use the AppearanceObject.BorderColor property value.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.AppearanceOptions.UseBorderColor")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Boolean? UseBorderColor{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to use the AppearanceObject.BackColor property value.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.AppearanceOptions.UseBackColor")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Boolean? UseBackColor{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to use the AppearanceObject.Image property value.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.AppearanceOptions.UseImage")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Boolean? UseImage{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to use the AppearanceObject.Font property value.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.AppearanceOptions.UseFont")]
[DevExpress.XtraEditors.DXCategoryAttribute("Font")]
System.Boolean? UseFont{get;set;}

}

[System.ComponentModel.EditorAttribute("DevExpress.Utils.Design.DxImageUriEditor, DevExpress.Design.v21.2, Version=21.2.8" +
    ".0, Culture=neutral, PublicKeyToken=b88d1754d700e49a",typeof(System.Drawing.Design.UITypeEditor))]
public interface IModelDevExpressUtils_DxImageUri:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

System.String Uri{get;set;}

}

[System.ComponentModel.DescriptionAttribute("Represents a collection of System.Drawing.Image objects and supports alpha channe" +
    "ls in images.")]
public interface IModelDevExpressUtils_ImageCollection:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
[System.ComponentModel.DescriptionAttribute("Gets or sets the size of images in the image collection.\nFor the SharedImageColle" +
    "ction, this property specifies the size of images fetched from an image strip (a" +
    "t design time or via the AddImageStrip and AddImageStripVertical methods).")]
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
System.Drawing.Size? ImageSize{get;set;}
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
[System.ComponentModel.DescriptionAttribute("Gets or sets the value that specifies that when requesting an image from the Imag" +
    "eCollection, another DPI-specific image corresponding to the requested image is " +
    "returned instead.")]
DevExpress.Utils.DefaultBoolean? IsDpiAware{get;set;}
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
[System.ComponentModel.DescriptionAttribute("Gets or sets the color to treat as transparent.")]
System.Drawing.Color? TransparentColor{get;set;}

}

[System.ComponentModel.TypeConverterAttribute(typeof(DevExpress.Utils.Design.BinaryTypeConverter))]
public interface IModelDevExpressUtils_ImageCollectionStreamer:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelDevExpressUtils_ImageCollection Collection{get;}

}

[System.ComponentModel.TypeConverterAttribute(typeof(DevExpress.Utils.ToolTipTypeConverter))]
public interface IModelDevExpressUtils_SuperToolTip:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets the padding (space between the content of the tooltip and its edge)." +
    "")]
System.Windows.Forms.Padding? Padding{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether HTML formatting is allowed in this tooltip.")]
DevExpress.Utils.DefaultBoolean? AllowHtmlText{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the tooltip width is fixed or corresponds to the content.")]
System.Boolean? FixedTooltipWidth{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the tooltip maximum width in pixels.")]
System.Int32? MaxWidth{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the distance between tooltip regions (title, content, footer, etc.).")]
System.Int32? DistanceBetweenItems{get;set;}

}

[System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
public interface IModelDevExpressUtils_TextOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets an object that contains the default formatting settings.")]
IModelSystemDrawing_StringFormat DefaultStringFormat{get;}
[System.ComponentModel.DescriptionAttribute("Gets or sets text wrapping mode.")]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.TextOptions.WordWrap")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
DevExpress.Utils.WordWrap? WordWrap{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to underline characters that are preceded with an ampersand " +
    "symbol (&). This option is supported by a set of controls.")]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.TextOptions.HotkeyPrefix")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
DevExpress.Utils.HKeyPrefix? HotkeyPrefix{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the horizontal alignment of text.")]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.TextOptions.HAlignment")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
DevExpress.Utils.HorzAlignment? HAlignment{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the vertical alignment of text.")]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.TextOptions.VAlignment")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
DevExpress.Utils.VertAlignment? VAlignment{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets text trimming mode.")]
[System.ComponentModel.DXDisplayNameAttribute(typeof(DevExpress.Utils.ResFinder),"PropertyNamesRes","DevExpress.Utils.TextOptions.Trimming")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
DevExpress.Utils.Trimming? Trimming{get;set;}

}


public interface IModelDevExpressUtils_ToolTipControlInfo:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the tooltip should be forcibly shown for the same visual ele" +
    "ment via the ToolTipController.ShowHint method.")]
DevExpress.Utils.DefaultBoolean? ForcedShow{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the shown tooltip hides when an end-user moves the mouse.")]
System.Boolean? HideHintOnMouseMove{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to options that specify the image displayed in the tooltip.")]
IModelDevExpressUtils_ToolTipItemImageOptions ImageOptions{get;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the tooltip location.")]
DevExpress.Utils.ToolTipLocation? ToolTipLocation{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the tooltip’s position in screen coordinates.")]
System.Drawing.Point? ToolTipPosition{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a SuperToolTip that will be displayed if the ToolTipControlInfo.Tool" +
    "TipType property is set to SuperTip")]
IModelDevExpressUtils_SuperToolTip SuperTip{get;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the interval that must pass before a tooltip is displayed.")]
System.Int32? Interval{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the tooltip’s text.")]
System.String Text{get;set;}

DevExpress.Utils.ToolTipAnchor? ToolTipAnchor{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the bounds of the object for which a tooltip is displayed.")]
System.Drawing.Rectangle? ObjectBounds{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the type of tooltip to be displayed.")]
DevExpress.Utils.ToolTipType? ToolTipType{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the tooltip’s title.")]
System.String Title{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a control shown within a flyout tooltip.")]
IModelSystemWindowsForms_Control FlyoutControl{get;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether a tooltip will be displayed immediately or after a delay.")]
System.Boolean? ImmediateToolTip{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the kind of predefined icon to display in a tooltip.")]
DevExpress.Utils.ToolTipIconType? IconType{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether HTML formatting is supported in tooltips.")]
DevExpress.Utils.DefaultBoolean? AllowHtmlText{get;set;}

}

[System.ComponentModel.DescriptionAttribute("Manages tooltips for a specific control or controls.")]
public interface IModelDevExpressUtils_ToolTipController:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[DevExpress.XtraEditors.DXCategoryAttribute("ToolTip")]
[System.ComponentModel.DescriptionAttribute("Gets or sets whether tooltips are anchored relative to the mouse pointer or relat" +
    "ive to the owning control. This property is not in effect if you handle the Tool" +
    "TipController.GetActiveObjectInfo event.")]
DevExpress.Utils.ToolTipAnchor? ToolTipAnchor{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether a displayed tooltip remains visible while the mouse cursor k" +
    "eeps moving (without pauses) towards the tooltip and while it hovers the tooltip" +
    ".")]
[DevExpress.XtraEditors.DXCategoryAttribute("Behavior")]
System.Boolean? KeepWhileHovered{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the type of tooltips displayed by the controller.")]
[DevExpress.XtraEditors.DXCategoryAttribute("ToolTip")]
DevExpress.Utils.ToolTipType? ToolTipType{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the component’s functionality is enabled.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Behavior")]
System.Boolean? Active{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the time interval between a visual element being hovered, and its to" +
    "oltip being shown on-screen.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Int32? InitialDelay{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the time interval that must pass before another hint is displayed if" +
    " another hint is currently visible.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Behavior")]
System.Int32? ReshowDelay{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the timeframe during which a tooltip is visible on-screen.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Behavior")]
System.Int32? AutoPopDelay{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the settings that control the appearance of a tooltip’s window and t" +
    "ext.")]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
IModelDevExpressUtils_AppearanceObject Appearance{get;}
[System.ComponentModel.DescriptionAttribute("Provide the settings that control the appearance of a tooltip’s title.")]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
IModelDevExpressUtils_AppearanceObject AppearanceTitle{get;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the tooltip location.")]
[DevExpress.XtraEditors.DXCategoryAttribute("ToolTip")]
DevExpress.Utils.ToolTipLocation? ToolTipLocation{get;set;}
[System.ComponentModel.DescriptionAttribute("Tests whether callout beaks are displayed for hints.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Boolean? ShowBeak{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the tooltip’s corners are rounded.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Boolean? Rounded{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the radius of the rounded corners of the tooltip window.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Int32? RoundRadius{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the tooltip icon size.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
DevExpress.Utils.ToolTipIconSize? IconSize{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the tooltips are shown shadowed.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Boolean? ShowShadow{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether HTML formatting tags can be used to format text in tooltips." +
    "")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
System.Boolean? AllowHtmlText{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether a tooltip is closed on a click.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Behavior")]
DevExpress.Utils.DefaultBoolean? CloseOnClick{get;set;}

}


public interface IModelDevExpressUtils_ToolTipItemImageOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.EditorAttribute("DevExpress.Utils.Design.DxImageUriEditor, DevExpress.Design.v21.2, Version=21.2.8" +
    ".0, Culture=neutral, PublicKeyToken=b88d1754d700e49a",typeof(System.Drawing.Design.UITypeEditor))]
IModelDevExpressUtils_DxImageUri ImageUri{get;}
[System.ComponentModel.EditorAttribute("DevExpress.Utils.Design.SvgImageEditor, DevExpress.Design.v21.2, Version=21.2.8.0" +
    ", Culture=neutral, PublicKeyToken=b88d1754d700e49a",typeof(System.Drawing.Design.UITypeEditor))]
IModelDevExpressUtilsSvg_SvgImage SvgImage{get;}

System.Drawing.Size? SvgImageSize{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the indentation of the text from the image.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Layout")]
System.Int32? ImageToTextDistance{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the index of the image in the ToolTipItemImageOptions.Images collect" +
    "ion.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
[DevExpress.Utils.ImageListAttribute("Images")]
[System.ComponentModel.EditorAttribute(typeof(DevExpress.Utils.Design.ImageIndexesEditor),typeof(System.Drawing.Design.UITypeEditor))]
System.Int32? ImageIndex{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the image is aligned at the left or right edge of the toolti" +
    "p.")]
[DevExpress.XtraEditors.DXCategoryAttribute("Layout")]
DevExpress.Utils.ToolTipImageAlignment? Alignment{get;set;}
[System.ComponentModel.DescriptionAttribute("")]
[DevExpress.XtraEditors.DXCategoryAttribute("Appearance")]
DevExpress.Utils.SvgImageColorizationMode? SvgImageColorizationMode{get;set;}

}


public interface IModelDevExpressUtilsDrawing_BorderPainter:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelDevExpressUtils_AppearanceDefault DefaultAppearance{get;}

}


public interface IModelDevExpressUtilsDrawing_FooterCellPainter:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelDevExpressUtilsDrawing_ObjectPainter ContentPainter{get;}

IModelDevExpressUtils_AppearanceDefault DefaultAppearance{get;}

}


public interface IModelDevExpressUtilsDrawing_FooterPanelPainter:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelDevExpressUtilsDrawing_ObjectPainter PanelButtonPainter{get;}

IModelDevExpressUtils_AppearanceDefault DefaultAppearance{get;}

}


public interface IModelDevExpressUtilsDrawing_GridGroupPanelPainter:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelDevExpressUtils_AppearanceDefault DefaultAppearance{get;}

}


public interface IModelDevExpressUtilsDrawing_HeaderObjectPainter:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelDevExpressUtilsDrawing_ObjectPainter ButtonPainter{get;}

System.Boolean? UseInnerElementsForBestHeight{get;set;}

IModelDevExpressUtils_AppearanceDefault DefaultAppearance{get;}

}


public interface IModelDevExpressUtilsDrawing_IndicatorObjectPainter:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelDevExpressUtils_ImageCollection DefaultImageList{get;}

IModelDevExpressUtilsDrawing_ObjectPainter ButtonPainter{get;}

IModelDevExpressUtils_ImageCollection ImageList{get;}

IModelDevExpressUtils_AppearanceDefault DefaultAppearance{get;}

}


public interface IModelDevExpressUtilsDrawing_ObjectPainter:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelDevExpressUtils_AppearanceDefault DefaultAppearance{get;}

}


public interface IModelDevExpressUtilsDrawing_ProgressBarObjectPainter:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelDevExpressUtils_AppearanceDefault DefaultAppearance{get;}

}


public interface IModelDevExpressUtilsDrawing_SizeGripObjectPainter:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelDevExpressUtils_AppearanceDefault DefaultAppearance{get;}

}


public interface IModelDevExpressUtilsSvg_SvgElement:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelDevExpressUtilsSvg_SvgElements Elements{get;}

IModelDevExpressUtilsSvg_SvgStyles Styles{get;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("fill")]
System.String Fill{get;set;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("opacity")]
System.Double? Opacity{get;set;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("fill-opacity")]
System.Double? FillOpacity{get;set;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("class")]
System.String StyleName{get;set;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("transform")]
[System.ComponentModel.TypeConverterAttribute(typeof(DevExpress.Utils.Svg.SvgTransformConverter))]
IModelDevExpressUtilsSvg_SvgTransforms Transformations{get;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("style")]
[System.ComponentModel.TypeConverterAttribute(typeof(DevExpress.Utils.Svg.SvgStyleConverter))]
IModelDevExpressUtilsSvg_SvgStyle Style{get;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("stroke")]
System.String Stroke{get;set;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("stroke-width")]
[System.ComponentModel.TypeConverterAttribute(typeof(DevExpress.Utils.Svg.SvgUnitConverter))]
IModelDevExpressUtilsSvg_SvgUnit StrokeWidth{get;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("stroke-linecap")]
DevExpress.Utils.Svg.SvgStrokeLineCap? StrokeLineCap{get;set;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("stroke-linejoin")]
DevExpress.Utils.Svg.SvgStrokeLineJoin? StrokeLineJoin{get;set;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("stroke-miterlimit")]
System.Double? StrokeMiterLimit{get;set;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("stroke-dasharray")]
[System.ComponentModel.TypeConverterAttribute(typeof(DevExpress.Utils.Svg.SvgUnitCollectionConverter))]
IModelDevExpressUtilsSvg_SvgUnits StrokeDashArray{get;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("stroke-dashoffset")]
[System.ComponentModel.TypeConverterAttribute(typeof(DevExpress.Utils.Svg.SvgUnitConverter))]
IModelDevExpressUtilsSvg_SvgUnit StrokeDashOffset{get;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("stroke-opacity")]
System.Double? StrokeOpacity{get;set;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("display")]
System.String Display{get;set;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("mask")]
IModelSystem_Uri Mask{get;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("clip-path")]
IModelSystem_Uri ClipPath{get;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("clip-rule")]
DevExpress.Utils.Svg.SvgClipRule? ClipRule{get;set;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("fill-rule")]
DevExpress.Utils.Svg.SvgFillRule? FillRule{get;set;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("font-family")]
System.String FontFamily{get;set;}
[DevExpress.Utils.Svg.SvgPropertyNameAliasAttribute("font-size")]
[System.ComponentModel.TypeConverterAttribute(typeof(DevExpress.Utils.Svg.SvgUnitConverter))]
IModelDevExpressUtilsSvg_SvgUnit FontSize{get;}

System.Double? Brightness{get;set;}

IModelDevExpressUtilsSvg_SvgStyle DefaultStyle{get;}

System.Boolean? UsePalette{get;set;}

}

public interface IModelDevExpressUtilsSvg_SvgElements:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelDevExpressUtilsSvg_SvgElement>{}

[System.ComponentModel.TypeConverterAttribute(typeof(DevExpress.Utils.Design.BinaryTypeConverter))]
public interface IModelDevExpressUtilsSvg_SvgImage:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelSystem_Strings UnknownTags{get;}

IModelDevExpressUtilsSvg_SvgStyle DefaultStyle{get;}

IModelDevExpressUtilsSvg_SvgElements Elements{get;}

IModelDevExpressUtilsSvg_SvgStyles Styles{get;}

System.Double? Width{get;set;}

System.Double? Height{get;set;}

System.Double? OffsetX{get;set;}

System.Double? OffsetY{get;set;}

}


public interface IModelDevExpressUtilsSvg_SvgStyle:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

System.String Name{get;set;}

}

public interface IModelDevExpressUtilsSvg_SvgStyles:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelDevExpressUtilsSvg_SvgStyle>{}


public interface IModelDevExpressUtilsSvg_SvgTransform:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelSystemDrawingDrawing2D_Matrix Matrix{get;}

}

public interface IModelDevExpressUtilsSvg_SvgTransforms:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelDevExpressUtilsSvg_SvgTransform>{}


public interface IModelDevExpressUtilsSvg_SvgUnit:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

System.Double? Value{get;set;}

System.Double? UnitValue{get;set;}

DevExpress.Utils.Svg.SvgUnitType? UnitType{get;set;}

}

public interface IModelDevExpressUtilsSvg_SvgUnits:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelDevExpressUtilsSvg_SvgUnit>{}


public interface IModelDevExpressXtraRichEdit_AnnotationOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Provides access to the options used to display comments in a document.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_CommentOptions Comments{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to the Track Changes options.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_TrackChangesOptions TrackChanges{get;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies whether to show annotations (comments and tracked changes) from all aut" +
    "hors.")]
System.Boolean? ShowAllAuthors{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to the visible reviewer’s names collection.")]
IModelSystem_Strings VisibleAuthors{get;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies the annotations’ author.")]
System.String Author{get;set;}

}


public interface IModelDevExpressXtraRichEdit_AuthenticationOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets a name used to authenticate a user, if document protection is enable" +
    "d.")]
System.String UserName{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets user group name used to authenticate a user if document protection i" +
    "s enabled.")]
System.String Group{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets email address used to authenticate a user if document protection is " +
    "enabled.")]
System.String EMail{get;set;}

}


public interface IModelDevExpressXtraRichEdit_AutoCorrectOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether AutoCorrect should scan for entries as you type and replace " +
    "them with designated text or image.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? ReplaceTextAsYouType{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether AutoCorrect should detect URI strings and format them as hyp" +
    "erlinks.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? DetectUrls{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether AutoCorrect should change the second of two initial capitals" +
    " to lowercase .")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? CorrectTwoInitialCapitals{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether AutoCorrect should correct misspelled words that are similar" +
    " to words in the dictionary that the spelling checker uses.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? UseSpellCheckerSuggestions{get;set;}

}


public interface IModelDevExpressXtraRichEdit_BookmarkOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether bookmarks are displayed in the document.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
DevExpress.XtraRichEdit.RichEditBookmarkVisibility? Visibility{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies what bookmarks to display in the PDF viewer’s Bookmarks pane when a doc" +
    "ument is exported to PDF.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
DevExpress.XtraRichEdit.PdfBookmarkDisplayMode? DisplayBookmarksInPdfNavigationPane{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the color used to indicate a bookmark in the document.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
System.Drawing.Color? Color{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies an action that will be performed after inserting a document range if th" +
    "at range contains a bookmark with the same name as one in the current document.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
DevExpress.XtraRichEdit.ConflictNameAction? ConflictNameResolution{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether creation of bookmarks with the same name but a different cas" +
    "e is allowed.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
System.Boolean? CaseSensitiveNames{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to display bookmarks without references in the PDF Viewer’s " +
    "Bookmarks navigation pane when the document is exported to PDF.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
System.Boolean? DisplayUnreferencedPdfBookmarks{get;set;}

}


public interface IModelDevExpressXtraRichEdit_CommentOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies the visibility mode of a document comment.")]
DevExpress.XtraRichEdit.RichEditCommentVisibility? Visibility{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Gets or sets the background color of the text to which a comment is attached.")]
System.Drawing.Color? Color{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Gets or sets whether commented document ranges are highlighted.")]
System.Boolean? HighlightCommentedRange{get;set;}

}


public interface IModelDevExpressXtraRichEdit_CopyPasteOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets options specifying how formatting is applied to pasted content.")]
DevExpress.XtraRichEdit.API.Native.InsertOptions? InsertOptions{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to retain section settings of a blank document after appendi" +
    "ng RTF content.")]
System.Boolean? MaintainDocumentSectionSettings{get;set;}

}


public interface IModelDevExpressXtraRichEdit_DataFormatOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to paste metafiles.")]
System.Boolean? AllowMetafile{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to paste images.")]
System.Boolean? AllowImage{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to copy or paste HTML content.")]
DevExpress.XtraRichEdit.RichEditClipboardMode? Html{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to copy or paste RTF content.")]
DevExpress.XtraRichEdit.RichEditClipboardMode? Rtf{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to copy or paste plain text.")]
DevExpress.XtraRichEdit.RichEditClipboardMode? PlainText{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies how images are included in the copied HTML content.")]
DevExpress.XtraRichEdit.HtmlImageSourceType? HtmlImageSourceType{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a path to the image files linked as pictures in the copied document " +
    "content.")]
System.String HtmlImagePath{get;set;}

}


public interface IModelDevExpressXtraRichEdit_DocumentCapabilitiesOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Gets or sets the availability of paragraph frames.")]
DevExpress.XtraRichEdit.DocumentCapability? ParagraphFrames{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the character formatting features availability.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? CharacterFormatting{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the paragraph formatting features availability.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? ParagraphFormatting{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document capability to handle inline pictures.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? InlinePictures{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the availability of the paragraph breaks.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Paragraphs{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the availability of the apply character style feature.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? CharacterStyle{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the availability of the apply paragraph style feature.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? ParagraphStyle{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the availability of the apply table style feature.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? TableStyle{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the hyperlink feature availability.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Hyperlinks{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the bookmark feature availability.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Bookmarks{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the availability of paragraph tab stops.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? ParagraphTabs{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document’s capability to insert tabs.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? TabSymbol{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document’s capability to handle document sections.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Sections{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets whether the headers and footers feature is allowed.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? HeadersFooters{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document’s capability to handle tables.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Tables{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document’s capability to contain footnotes (notes placed at the " +
    "end of a document).")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? FootNotes{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document’s capability to contain endnotes (notes placed at the e" +
    "nd of the text).")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? EndNotes{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document’s capability to handle floating objects.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? FloatingObjects{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether OLE objects are available.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? OleObjects{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether ActiveX controls are available.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? ActiveX{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether watermarks are available.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Watermarks{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether embedded charts are available.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Charts{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether macros are available.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Macros{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document’s capability to display and work with comments.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Comments{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Gets or sets the document’s capability to recognize and work with fields.")]
DevExpress.XtraRichEdit.DocumentCapability? Fields{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document capability to handle inline shapes (objects in the docu" +
    "ment’s text layer).")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? InlineShapes{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the Track Changes feature is available.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? TrackChanges{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether paragraph borders are available.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? ParagraphBorders{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to options specifying the availability of bulleted and numbered l" +
    "ists in the document.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_NumberingOptions Numbering{get;}

}


public interface IModelDevExpressXtraRichEdit_DocumentSaveOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets the file name used by default for a new document, when saving or loa" +
    "ding a document which has not been previously saved.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.String DefaultFileName{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the file name into which the document is saved or from which it is l" +
    "oaded.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.String CurrentFileName{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the default file format used for saving a newly created document.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentFormat? DefaultFormat{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the file format into which the document is saved or from which it is" +
    " loaded.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentFormat? CurrentFormat{get;set;}

}


public interface IModelDevExpressXtraRichEdit_DocumentSearchOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets the maximum length of a string that can be obtained in a regular exp" +
    "ression search.")]
System.Int32? RegExResultMaxGuaranteedLength{get;set;}

}


public interface IModelDevExpressXtraRichEdit_DraftViewLayoutOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets how to apply horizontal table indents.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? MatchHorizontalTableIndentsToTextEdge{get;set;}

}


public interface IModelDevExpressXtraRichEdit_FieldOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets when the document fields should be highlighted.")]
DevExpress.XtraRichEdit.FieldsHighlightMode? HighlightMode{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the color used to highlight the document fields.")]
System.Drawing.Color? HighlightColor{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the current culture’s short date and time display settings s" +
    "hould be used to format DateTime value for display.")]
System.Boolean? UseCurrentCultureDateTimeFormat{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether locked fields can be updated.")]
DevExpress.XtraRichEdit.UpdateLockedFields? UpdateLockedFields{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the DOCVARIABLE field value is cleared when it cannot be cal" +
    "culated.")]
System.Boolean? ClearUnhandledDocVariableField{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether to refresh the values of document variables when the document i" +
    "s printed or exported to PDF.")]
System.Boolean? UpdateDocVariablesBeforePrint{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether to refresh the values of document variables when they are to be" +
    " copied to the Clipboard.")]
System.Boolean? UpdateDocVariablesBeforeCopy{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether document fields contained in the pasted range should be upda" +
    "ted.")]
System.Boolean? UpdateFieldsOnPaste{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to update hyperlinks without the result text.")]
System.Boolean? UpdateHyperlinksOnLoad{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to throw exception if a field formatting switch is not recog" +
    "nized.")]
System.Boolean? ThrowExceptionOnInvalidFormatSwitch{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the Update method updates fields contained in text boxes.")]
System.Boolean? UpdateFieldsInTextBoxes{get;set;}

}


public interface IModelDevExpressXtraRichEdit_FontSubstitutionOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets the ASCII font used for substitution.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.String Ascii{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the High Ansi font name used for substitution.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.String HighAnsi{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the Complex Script font used for substitution.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.String ComplexScript{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the East Asian Unicode font used for substitution.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.String EastAsia{get;set;}

}


public interface IModelDevExpressXtraRichEdit_FormattingMarkVisibilityOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the paragraph mark should be displayed.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
DevExpress.XtraRichEdit.RichEditFormattingMarkVisibility? ParagraphMark{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether tab characters should be made visible.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
DevExpress.XtraRichEdit.RichEditFormattingMarkVisibility? TabCharacter{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether space characters should be made visible.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
DevExpress.XtraRichEdit.RichEditFormattingMarkVisibility? Space{get;set;}
[System.ComponentModel.DescriptionAttribute("Not in use for the RichEditControl.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
DevExpress.XtraRichEdit.RichEditFormattingMarkVisibility? Separator{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the visibility of the hidden text.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
DevExpress.XtraRichEdit.RichEditFormattingMarkVisibility? HiddenText{get;set;}

}


public interface IModelDevExpressXtraRichEdit_HorizontalRulerOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether tab stops are displayed in the horizontal ruler.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? ShowTabs{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the left indent marker is visible.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? ShowLeftIndent{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the right indent marker is visible.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? ShowRightIndent{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the ruler is shown.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.RichEditRulerVisibility? Visibility{get;set;}

}


public interface IModelDevExpressXtraRichEdit_HorizontalScrollbarOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets a value that specifies the visibility of a scroll bar.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.RichEditScrollbarVisibility? Visibility{get;set;}

}


public interface IModelDevExpressXtraRichEdit_HyperlinkOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the hyperlink should display a tooltip.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? ShowToolTip{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the modifier keys (CTRL, SHIFT, and ALT) required to activate a hype" +
    "rlink.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.EditorAttribute("DevExpress.XtraRichEdit.Design.ModifiersEditor, DevExpress.XtraRichEdit.v21.2.Des" +
    "ign, Version=21.2.8.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a","System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neut" +
    "ral, PublicKeyToken=b03f5f7f11d50a3a")]
DevExpress.Portable.Input.PortableKeys? ModifierKeys{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the automatic correction of the hyperlink URI is enabled.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? EnableUriCorrection{get;set;}

}

[System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
public interface IModelDevExpressXtraRichEdit_MailMergeCustomSeparators:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets a symbol displayed in the field result instead of a decimal separato" +
    "r specified in the numeric format switch.")]
System.String FieldResultDecimalSeparator{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a symbol displayed in the field result instead of a group separator " +
    "specified in the numeric format switch.")]
System.String FieldResultGroupSeparator{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a symbol used as a decimal separator in the numeric format switch of" +
    " a document field.")]
System.String MaskDecimalSeparator{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a symbol used as a group separator in the numeric format switch of a" +
    " document field.")]
System.String MaskGroupSeparator{get;set;}

}


public interface IModelDevExpressXtraRichEdit_NumberingOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets the availability of operations with bulleted lists in the document.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Bulleted{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the availability of operations with simple numbered lists in the doc" +
    "ument.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Simple{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the availability of operations with multilevel lists in the document" +
    ".")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? MultiLevel{get;set;}

}


public interface IModelDevExpressXtraRichEdit_PrintingOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the page background is printed in printouts or exported in g" +
    "raphic formats such as PDF.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
System.Boolean? EnablePageBackgroundOnPrint{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to print the background of the document’s margin with commen" +
    "t balloons.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
System.Boolean? EnableCommentBackgroundOnPrint{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to print the background color of the comment balloons.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
System.Boolean? EnableCommentFillOnPrint{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether fields are automatically updated before a document is printe" +
    "d.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
System.Boolean? UpdateDocVariablesBeforePrint{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the user interface of the Print Preview form.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
DevExpress.XtraRichEdit.PrintPreviewFormKind? PrintPreviewFormKind{get;set;}

}


public interface IModelDevExpressXtraRichEdit_PrintLayoutViewLayoutOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{


}


public interface IModelDevExpressXtraRichEdit_RangePermissionOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the ranges with permissions are visually indicated.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
DevExpress.XtraRichEdit.RichEditRangePermissionVisibility? Visibility{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the color of the visual marks (brackets) that indicate the start and" +
    " the end of a range with permission in a document with protection disabled.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
System.Drawing.Color? BracketsColor{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the color of the visual marks (brackets) that indicate the start and" +
    " the end of each editable range in a protected document with protection enabled." +
    "")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
System.Drawing.Color? HighlightBracketsColor{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the color used to highlight each editable range in a protected docum" +
    "ent with protection enabled.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
System.Drawing.Color? HighlightColor{get;set;}

}


public interface IModelDevExpressXtraRichEdit_RichEditBehaviorOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the right-to-left text direction is permitted.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? RightToLeftTextDirection{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the Distribute and Thai Distribute alignment operations " +
    "are permitted.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? EastAsianTextAlignment{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the Drag operation is permitted.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Drag{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the Drop operation is permitted.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Drop{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the Copy operation is permitted.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Copy{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the printing operations are permitted.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Printing{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the Save Document As… operation is permitted.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? SaveAs{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the Save Document operation is permitted.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Save{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the Zoom operation is permitted.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Zooming{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the RichEdit popup menu can be displayed.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? ShowPopupMenu{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the Office Scrolling feature is enabled.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? OfficeScrolling{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the touch device input is analyzed.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Touch{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether a page break is inserted next to the specified position or i" +
    "n the new line.")]
DevExpress.XtraRichEdit.PageBreakInsertMode? PageBreakInsertMode{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the lower limit of document zooming.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Single? MinZoomFactor{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the upper limit of document zooming.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Single? MaxZoomFactor{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the Paste operation is permitted.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Paste{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the Cut operation is permitted.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Cut{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the Create New Document operation is permitted.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? CreateNew{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the Open Document operation is permitted.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Open{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the substitute character that is used to replace the line break in past" +
    "ed HTML.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.LineBreakSubstitute? PasteLineBreakSubstitution{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to enable document encryption.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentCapability? Encrypt{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not to paste a cell from the Clipboard as plain text.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? PasteSingleCellAsText{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to update the document properties automatically.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? DocumentPropertiesAutoUpdate{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the settings applied to the default font of a RichEdit control.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.RichEditBaseValueSource? FontSource{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the color settings applied to the default font of a RichEdit control.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.RichEditBaseValueSource? ForeColorSource{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies which character(s) to insert when pressing the TAB key.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.String TabMarker{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not to use a font substitution for displaying characters tha" +
    "t are missing in the current font.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? UseFontSubstitution{get;set;}
[System.ComponentModel.DescriptionAttribute("Retrieves font substitution options.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_FontSubstitutionOptions FontSubstitution{get;}
[System.ComponentModel.DescriptionAttribute("Specifies whether to use theme fonts to retrieve default document font informatio" +
    "n.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? UseThemeFonts{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the overtype mode is allowed.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? OvertypeAllowed{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the ClearFormattingCommand removes text highlighting.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? KeepTextHighlightingOnClearFormatting{get;set;}

}


public interface IModelDevExpressXtraRichEdit_RichEditControlOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Provides access to the options specific for the vertical scrollbar of the RichEdi" +
    "tControl.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_VerticalScrollbarOptions VerticalScrollbar{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to the options specific to the horizontal scrollbar of the RichEd" +
    "itControl.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_HorizontalScrollbarOptions HorizontalScrollbar{get;}
[System.ComponentModel.DescriptionAttribute("Gets document field options.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_FieldOptions Fields{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to the default mail merge options.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_RichEditMailMergeOptions MailMerge{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to an object used to specify document restrictions.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_DocumentCapabilitiesOptions DocumentCapabilities{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to RichEditControl options that affect layout and display.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_RichEditLayoutOptions Layout{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to an object that enables you to apply restrictions on different " +
    "editor operations.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_RichEditBehaviorOptions Behavior{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to the options used for export to different formats.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_RichEditDocumentExportOptions Export{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to the options used for import from different formats.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_RichEditDocumentImportOptions Import{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to options specific to hyperlinks.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_HyperlinkOptions Hyperlinks{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to an object used to specify different options for bookmarks in t" +
    "he document.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_BookmarkOptions Bookmarks{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to an object used to specify various options for ranges with perm" +
    "issions in the document.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_RangePermissionOptions RangePermissions{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to options for displaying annotations in a document.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_AnnotationOptions Annotations{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to the options used for searching within a document.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_DocumentSearchOptions Search{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to the control’s document saving options.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_DocumentSaveOptions DocumentSaveOptions{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to an object used to specify how formatting marks are shown in th" +
    "e document.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_FormattingMarkVisibilityOptions FormattingMarkVisibility{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to an object used to specify different options for tables in the " +
    "document.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_TableOptions TableOptions{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to options specific for the vertical ruler.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_VerticalRulerOptions VerticalRuler{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to options specific to the horizontal ruler element of the RichEd" +
    "itControl.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_HorizontalRulerOptions HorizontalRuler{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to an object used to specify the identity parameters for range ed" +
    "iting permissions.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_AuthenticationOptions Authentication{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to options that enable you to configure autocorrect features.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_AutoCorrectOptions AutoCorrect{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to options specific to printing.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_PrintingOptions Printing{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to options useful in certain scenarios for inserting the content " +
    "of one document into another.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_CopyPasteOptions CopyPaste{get;}
[System.ComponentModel.DescriptionAttribute("Gets the data formats currently available on the Clipboard.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_DataFormatOptions ClipboardFormats{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to options that determine how the spell checker processes the doc" +
    "ument text.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_SpellCheckerOptions SpellChecker{get;}

}


public interface IModelDevExpressXtraRichEdit_RichEditLayoutOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Provides access to options specific for the layout of the Draft view.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_DraftViewLayoutOptions DraftView{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to options specific for the layout of the PrintLayout view.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_PrintLayoutViewLayoutOptions PrintLayoutView{get;}
[System.ComponentModel.DescriptionAttribute("Provides access to options specific for the layout of the Simple view.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_SimpleViewLayoutOptions SimpleView{get;}

}


public interface IModelDevExpressXtraRichEdit_RichEditMailMergeOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Specifies the member of a mail-merge document‘s data source.")]
[System.ComponentModel.EditorAttribute("System.Windows.Forms.Design.DataMemberListEditor, System.Design","System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neut" +
    "ral, PublicKeyToken=b03f5f7f11d50a3a")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.String DataMember{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether to display field results or field codes in a mail-merge documen" +
    "t.")]
System.Boolean? ViewMergedData{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the last paragraph of the inserted document is kept in t" +
    "he resulting document.")]
System.Boolean? KeepLastParagraph{get;set;}
[System.ComponentModel.DescriptionAttribute("Allows you to specify group and decimal separators used in a numeric format switc" +
    "h.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEdit_MailMergeCustomSeparators CustomSeparators{get;}

}


public interface IModelDevExpressXtraRichEdit_SimpleViewLayoutOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets how horizontal table indents are applied.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? MatchHorizontalTableIndentsToTextEdge{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether to resize the table cells if their contents extend the cell mar" +
    "gins.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? ResizeTablesToFitContent{get;set;}

}


public interface IModelDevExpressXtraRichEdit_SpellCheckerOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the spell checker should ignore language settings for docume" +
    "nt ranges and determine the language automatically.")]
System.Boolean? AutoDetectDocumentCulture{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the spell checker should ignore “no-proof” settings for text" +
    " ranges in a document.")]
System.Boolean? IgnoreNoProof{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the number of errors at which the spell check stops.")]
System.Int32? UpperErrorLimit{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the number of misspelled words at which spell check is resumed after" +
    " it has been stopped due to the high number of misspellings.")]
System.Int32? LowerErrorLimit{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether the spell check stops and the list of misspelled words is clear" +
    "ed when the number of found misspellings exceeds a predefined limit.")]
System.Boolean? ClearErrorsAfterLimitExceeded{get;set;}

}


public interface IModelDevExpressXtraRichEdit_TableOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the boundaries of cells without borders applied, are visible" +
    ".")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
DevExpress.XtraRichEdit.RichEditTableGridLinesVisibility? GridLines{get;set;}

}


public interface IModelDevExpressXtraRichEdit_TrackChangesOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies a color used to indicate inserted content.")]
DevExpress.XtraRichEdit.RevisionColor? InsertionColor{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies a color to indicate deletions.")]
DevExpress.XtraRichEdit.RevisionColor? DeletionColor{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies a color used to indicate the content moved from the target position.")]
DevExpress.XtraRichEdit.RevisionColor? MovedFromColor{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies a color used to indicate the content moved to the target position.")]
DevExpress.XtraRichEdit.RevisionColor? MovedToColor{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies a color used to indicate changed format options.")]
DevExpress.XtraRichEdit.RevisionColor? FormattingColor{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies the color used to indicate inserted table cells.")]
DevExpress.XtraRichEdit.TableCellRevisionColor? InsertedCellColor{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies a color to indicate a deleted table cell.")]
DevExpress.XtraRichEdit.TableCellRevisionColor? DeletedCellColor{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies a color used to indicate merged cells.")]
DevExpress.XtraRichEdit.TableCellRevisionColor? MergedCellColor{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies a color used to indicate split cells.")]
DevExpress.XtraRichEdit.TableCellRevisionColor? SplitCellColor{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies how to display document changes.")]
DevExpress.XtraRichEdit.DisplayForReviewMode? DisplayForReviewMode{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies the style of the markup used to indicate inserted content.")]
DevExpress.XtraRichEdit.DisplayInsertionStyle? DisplayInsertionStyle{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies the style of the markup used to indicate deleted content.")]
DevExpress.XtraRichEdit.DisplayDeletionStyle? DisplayDeletionStyle{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies the position of changed lines’ indicators.")]
DevExpress.XtraRichEdit.ChangedLinesMarkupPosition? ChangedLinesMarkupPosition{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies the style of the markup used to indicate the content moved from this po" +
    "sition.")]
DevExpress.XtraRichEdit.DisplayDeletionStyle? DisplayMovedFromStyle{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies the style of the markup used to indicate the content moved to this posi" +
    "tion.")]
DevExpress.XtraRichEdit.DisplayInsertionStyle? DisplayMovedToStyle{get;set;}
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies the markup used to indicate changed format options.")]
DevExpress.XtraRichEdit.DisplayFormatting? DisplayFormatting{get;set;}

}


public interface IModelDevExpressXtraRichEdit_VerticalRulerOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the ruler is shown.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.RichEditRulerVisibility? Visibility{get;set;}

}


public interface IModelDevExpressXtraRichEdit_VerticalScrollbarOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets a value that specifies the visibility of a scroll bar.")]
[DevExpress.Utils.Serializing.XtraSerializableProperty()]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.RichEditScrollbarVisibility? Visibility{get;set;}

}


public interface IModelDevExpressXtraRichEditAPILayout_DocumentLayout:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

System.Boolean? IsDocumentFormattingCompleted{get;set;}

}


public interface IModelDevExpressXtraRichEditAPINative_IHyphenationDictionary:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelSystemGlobalization_CultureInfo CultureInfo{get;}

System.String DictionaryPath{get;set;}

}

public interface IModelDevExpressXtraRichEditAPINative_IHyphenationDictionarys:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelDevExpressXtraRichEditAPINative_IHyphenationDictionary>{}


public interface IModelDevExpressXtraRichEditExport_DocDocumentExporterCompatibilityOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("")]
System.Boolean? AllowNonLinkedListDefinitions{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_DocDocumentExporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Provides access to options affecting the compatibility of exported files with dif" +
    "ferent DOC editors.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_DocDocumentExporterCompatibilityOptions Compatibility{get;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document properties being exported.")]
DevExpress.XtraRichEdit.Export.DocumentPropertyNames? ExportedDocumentProperties{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_DocmDocumentExporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Specifies the directory in which the document images should be saved.")]
System.Boolean? AlternateImageFolder{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether bookmark names longer than 40 characters should be automatic" +
    "ally renamed in the exported file.")]
System.Boolean? LimitBookmarkNameTo40Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether style with names longer than 253 characters should be automa" +
    "tically renamed in the exported file.")]
System.Boolean? LimitStyleNameTo253Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether font names longer than 31 characters should be truncated in " +
    "the exported file.")]
System.Boolean? LimitFontNameTo31Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to export the document compatibility settings.")]
System.Boolean? ExportCompatibilitySettings{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the alternate names (aliases) for built-in styles are allowe" +
    "d in the exported file.")]
System.Boolean? AllowAlternateStyleNames{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document properties being exported.")]
DevExpress.XtraRichEdit.Export.DocumentPropertyNames? ExportedDocumentProperties{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_DotDocumentExporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Provides access to options affecting the compatibility of exported files with dif" +
    "ferent DOC editors.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_DocDocumentExporterCompatibilityOptions Compatibility{get;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document properties being exported.")]
DevExpress.XtraRichEdit.Export.DocumentPropertyNames? ExportedDocumentProperties{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_DotmDocumentExporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Specifies the directory in which the document images should be saved.")]
System.Boolean? AlternateImageFolder{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether bookmark names longer than 40 characters should be automatic" +
    "ally renamed in the exported file.")]
System.Boolean? LimitBookmarkNameTo40Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether style with names longer than 253 characters should be automa" +
    "tically renamed in the exported file.")]
System.Boolean? LimitStyleNameTo253Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether font names longer than 31 characters should be truncated in " +
    "the exported file.")]
System.Boolean? LimitFontNameTo31Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to export the document compatibility settings.")]
System.Boolean? ExportCompatibilitySettings{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the alternate names (aliases) for built-in styles are allowe" +
    "d in the exported file.")]
System.Boolean? AllowAlternateStyleNames{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document properties being exported.")]
DevExpress.XtraRichEdit.Export.DocumentPropertyNames? ExportedDocumentProperties{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_DotxDocumentExporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Specifies the directory in which the document images should be saved.")]
System.Boolean? AlternateImageFolder{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether bookmark names longer than 40 characters should be automatic" +
    "ally renamed in the exported file.")]
System.Boolean? LimitBookmarkNameTo40Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether style with names longer than 253 characters should be automa" +
    "tically renamed in the exported file.")]
System.Boolean? LimitStyleNameTo253Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether font names longer than 31 characters should be truncated in " +
    "the exported file.")]
System.Boolean? LimitFontNameTo31Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to export the document compatibility settings.")]
System.Boolean? ExportCompatibilitySettings{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the alternate names (aliases) for built-in styles are allowe" +
    "d in the exported file.")]
System.Boolean? AllowAlternateStyleNames{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document properties being exported.")]
DevExpress.XtraRichEdit.Export.DocumentPropertyNames? ExportedDocumentProperties{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_FlatOpcDocumentExporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Specifies the directory in which the document images should be saved.")]
System.Boolean? AlternateImageFolder{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether bookmark names longer than 40 characters should be automatic" +
    "ally renamed in the exported file.")]
System.Boolean? LimitBookmarkNameTo40Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether style with names longer than 253 characters should be automa" +
    "tically renamed in the exported file.")]
System.Boolean? LimitStyleNameTo253Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether font names longer than 31 characters should be truncated in " +
    "the exported file.")]
System.Boolean? LimitFontNameTo31Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to export the document compatibility settings.")]
System.Boolean? ExportCompatibilitySettings{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the alternate names (aliases) for built-in styles are allowe" +
    "d in the exported file.")]
System.Boolean? AllowAlternateStyleNames{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document properties being exported.")]
DevExpress.XtraRichEdit.Export.DocumentPropertyNames? ExportedDocumentProperties{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_FlatOpcMacroEnabledDocumentExporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Specifies the directory in which the document images should be saved.")]
System.Boolean? AlternateImageFolder{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether bookmark names longer than 40 characters should be automatic" +
    "ally renamed in the exported file.")]
System.Boolean? LimitBookmarkNameTo40Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether style with names longer than 253 characters should be automa" +
    "tically renamed in the exported file.")]
System.Boolean? LimitStyleNameTo253Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether font names longer than 31 characters should be truncated in " +
    "the exported file.")]
System.Boolean? LimitFontNameTo31Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to export the document compatibility settings.")]
System.Boolean? ExportCompatibilitySettings{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the alternate names (aliases) for built-in styles are allowe" +
    "d in the exported file.")]
System.Boolean? AllowAlternateStyleNames{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document properties being exported.")]
DevExpress.XtraRichEdit.Export.DocumentPropertyNames? ExportedDocumentProperties{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_FlatOpcMacroEnabledTemplateDocumentExporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Specifies the directory in which the document images should be saved.")]
System.Boolean? AlternateImageFolder{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether bookmark names longer than 40 characters should be automatic" +
    "ally renamed in the exported file.")]
System.Boolean? LimitBookmarkNameTo40Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether style with names longer than 253 characters should be automa" +
    "tically renamed in the exported file.")]
System.Boolean? LimitStyleNameTo253Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether font names longer than 31 characters should be truncated in " +
    "the exported file.")]
System.Boolean? LimitFontNameTo31Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to export the document compatibility settings.")]
System.Boolean? ExportCompatibilitySettings{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the alternate names (aliases) for built-in styles are allowe" +
    "d in the exported file.")]
System.Boolean? AllowAlternateStyleNames{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document properties being exported.")]
DevExpress.XtraRichEdit.Export.DocumentPropertyNames? ExportedDocumentProperties{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_FlatOpcTemplateDocumentExporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Specifies the directory in which the document images should be saved.")]
System.Boolean? AlternateImageFolder{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether bookmark names longer than 40 characters should be automatic" +
    "ally renamed in the exported file.")]
System.Boolean? LimitBookmarkNameTo40Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether style with names longer than 253 characters should be automa" +
    "tically renamed in the exported file.")]
System.Boolean? LimitStyleNameTo253Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether font names longer than 31 characters should be truncated in " +
    "the exported file.")]
System.Boolean? LimitFontNameTo31Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to export the document compatibility settings.")]
System.Boolean? ExportCompatibilitySettings{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the alternate names (aliases) for built-in styles are allowe" +
    "d in the exported file.")]
System.Boolean? AllowAlternateStyleNames{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document properties being exported.")]
DevExpress.XtraRichEdit.Export.DocumentPropertyNames? ExportedDocumentProperties{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_HtmlDocumentExporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets how the lists are represented in the resulting document.")]
DevExpress.XtraRichEdit.Export.Html.HtmlNumberingListExportFormat? HtmlNumberingListExportFormat{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets how the style sheets properties are exported.")]
DevExpress.XtraRichEdit.Export.Html.CssPropertiesExportType? CssPropertiesExportType{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies how the links to external content are saved in the exported document.")]
DevExpress.XtraRichEdit.Export.Html.UriExportType? UriExportType{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the root tag of the HTML document to start the export.")]
DevExpress.XtraRichEdit.Export.Html.ExportRootTag? ExportRootTag{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets how the current image size is preserved in the resulting HTML output" +
    ".")]
DevExpress.XtraRichEdit.Export.Html.ExportImageSize? ExportImageSize{get;set;}
[System.ComponentModel.DescriptionAttribute("Fixes incorrect export of images in metafile formats.")]
System.Boolean? KeepExternalImageSize{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the Paragraph.LineSpacing setting is exported in HTML.")]
System.Boolean? KeepLineSpacing{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether images are embedded in an HTML document.")]
System.Boolean? EmbedImages{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the Document.DefaultCharacterProperties formatting is ex" +
    "ported in HTML style sheet.")]
System.Boolean? DefaultCharacterPropertiesExportToCss{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a character or a string used to replace a tab symbol when exporting " +
    "a document in HTML format.")]
System.String TabMarker{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the measurement unit to specify font size when exporting to HTML.")]
DevExpress.XtraRichEdit.Export.Html.HtmlFontUnit? FontUnit{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether Table of Contents entries are underlined in a document expor" +
    "ted to HTML format.")]
System.Boolean? UnderlineTocHyperlinks{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the graphics resolution used to save images when a document is expor" +
    "ted in HTML format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Int32? OverrideImageResolution{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the format string that specifies how the footnote number is transfor" +
    "med into a string to construct the name of the footnote reference in exported do" +
    "cument.")]
System.String FootNoteNumberStringFormat{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the format string that specifies how the endnote number is transform" +
    "ed into a string to construct the name of the endnote reference in exported docu" +
    "ment.")]
System.String EndNoteNumberStringFormat{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the string used to construct the name of the footnote reference in a" +
    "n exported document.")]
System.String FootNoteNamePrefix{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the exported HTML conforms with HTML5 specification.")]
System.Boolean? UseHtml5{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether paragraphs with different Paragraph.OutlineLevel values are " +
    "exported as text enclosed in the <P> tag.")]
System.Boolean? IgnoreParagraphOutlineLevel{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether missing fonts are substituted.")]
System.Boolean? UseFontSubstitution{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to disable writing font settings into *<li>* tags when expor" +
    "ting a document to the HTML format.")]
System.Boolean? ExportListItemStyle{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to ignore the paragraph’s hanging indent value when exportin" +
    "g the numbering and bulleted lists to HTML.")]
System.Boolean? IgnoreHangingIndentOnNumberingList{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the string used to construct the name of the endnote reference in an" +
    " exported document.")]
System.String EndNoteNamePrefix{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document properties being exported.")]
DevExpress.XtraRichEdit.Export.DocumentPropertyNames? ExportedDocumentProperties{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_MhtDocumentExporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets how the lists are represented in the resulting document.")]
DevExpress.XtraRichEdit.Export.Html.HtmlNumberingListExportFormat? HtmlNumberingListExportFormat{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets how the style sheets properties are exported.")]
DevExpress.XtraRichEdit.Export.Html.CssPropertiesExportType? CssPropertiesExportType{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies how the links to external content are saved in the exported document.")]
DevExpress.XtraRichEdit.Export.Html.UriExportType? UriExportType{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the root tag of the HTML document to start the export.")]
DevExpress.XtraRichEdit.Export.Html.ExportRootTag? ExportRootTag{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets how the current image size is preserved in the resulting HTML output" +
    ".")]
DevExpress.XtraRichEdit.Export.Html.ExportImageSize? ExportImageSize{get;set;}
[System.ComponentModel.DescriptionAttribute("Fixes incorrect export of images in metafile formats.")]
System.Boolean? KeepExternalImageSize{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the Paragraph.LineSpacing setting is exported in HTML.")]
System.Boolean? KeepLineSpacing{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not the Document.DefaultCharacterProperties formatting is ex" +
    "ported in HTML style sheet.")]
System.Boolean? DefaultCharacterPropertiesExportToCss{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a character or a string used to replace a tab symbol when exporting " +
    "a document in HTML format.")]
System.String TabMarker{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the measurement unit to specify font size when exporting to HTML.")]
DevExpress.XtraRichEdit.Export.Html.HtmlFontUnit? FontUnit{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether Table of Contents entries are underlined in a document expor" +
    "ted to HTML format.")]
System.Boolean? UnderlineTocHyperlinks{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the graphics resolution used to save images when a document is expor" +
    "ted in HTML format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Int32? OverrideImageResolution{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the format string that specifies how the footnote number is transfor" +
    "med into a string to construct the name of the footnote reference in exported do" +
    "cument.")]
System.String FootNoteNumberStringFormat{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the format string that specifies how the endnote number is transform" +
    "ed into a string to construct the name of the endnote reference in exported docu" +
    "ment.")]
System.String EndNoteNumberStringFormat{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the string used to construct the name of the footnote reference in a" +
    "n exported document.")]
System.String FootNoteNamePrefix{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the exported HTML conforms with HTML5 specification.")]
System.Boolean? UseHtml5{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether paragraphs with different Paragraph.OutlineLevel values are " +
    "exported as text enclosed in the <P> tag.")]
System.Boolean? IgnoreParagraphOutlineLevel{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether missing fonts are substituted.")]
System.Boolean? UseFontSubstitution{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to disable writing font settings into *<li>* tags when expor" +
    "ting a document to the HTML format.")]
System.Boolean? ExportListItemStyle{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to ignore the paragraph’s hanging indent value when exportin" +
    "g the numbering and bulleted lists to HTML.")]
System.Boolean? IgnoreHangingIndentOnNumberingList{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the string used to construct the name of the endnote reference in an" +
    " exported document.")]
System.String EndNoteNamePrefix{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document properties being exported.")]
DevExpress.XtraRichEdit.Export.DocumentPropertyNames? ExportedDocumentProperties{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_OpenDocumentExporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets the document properties being exported.")]
DevExpress.XtraRichEdit.Export.DocumentPropertyNames? ExportedDocumentProperties{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_OpenXmlDocumentExporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Specifies the directory in which the document images should be saved.")]
System.Boolean? AlternateImageFolder{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether bookmark names longer than 40 characters should be automatic" +
    "ally renamed in the exported file.")]
System.Boolean? LimitBookmarkNameTo40Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether style with names longer than 253 characters should be automa" +
    "tically renamed in the exported file.")]
System.Boolean? LimitStyleNameTo253Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether font names longer than 31 characters should be truncated in " +
    "the exported file.")]
System.Boolean? LimitFontNameTo31Chars{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to export the document compatibility settings.")]
System.Boolean? ExportCompatibilitySettings{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the alternate names (aliases) for built-in styles are allowe" +
    "d in the exported file.")]
System.Boolean? AllowAlternateStyleNames{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document properties being exported.")]
DevExpress.XtraRichEdit.Export.DocumentPropertyNames? ExportedDocumentProperties{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_PlainTextDocumentExporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to export hidden text as plain text.")]
System.Boolean? ExportHiddenText{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether document lists are distinguished by bullet symbols or number" +
    "s in the text output.")]
System.Boolean? ExportBulletsAndNumbering{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to export a numbering list level separator (a character that" +
    " follows the number or bullet symbol).")]
System.Boolean? ExportListLevelSeparator{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a character or a string used to mark the start of a field code in th" +
    "e resulting text.")]
System.String FieldCodeStartMarker{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a character or a string used to mark the end of a field code in the " +
    "resulting text.")]
System.String FieldCodeEndMarker{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a character or a string used to mark the end of a field result in th" +
    "e exported text.")]
System.String FieldResultEndMarker{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the format string that specifies how the footnote number is transfor" +
    "med into a plain text string.")]
System.String FootNoteNumberStringFormat{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the format string that specifies how the endnote number is transform" +
    "ed into a plain text string.")]
System.String EndNoteNumberStringFormat{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the string used to mark the footnote in the resulting plain text.")]
System.String FootNoteSeparator{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the string used to mark the endnote in the resulting plain text.")]
System.String EndNoteSeparator{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to append a new line symbol to the exported text if the rang" +
    "e for export ends with a paragraph mark.")]
DevExpress.XtraRichEdit.Export.PlainText.ExportFinalParagraphMark? ExportFinalParagraphMark{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document properties being exported.")]
DevExpress.XtraRichEdit.Export.DocumentPropertyNames? ExportedDocumentProperties{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_RichEditDocumentExportOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Obtains options specific for export to RTF format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_RtfDocumentExporterOptions Rtf{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for export to plain text format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_PlainTextDocumentExporterOptions PlainText{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for export to HTML format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_HtmlDocumentExporterOptions Html{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for export to Mht (“Web Archive”) format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_MhtDocumentExporterOptions Mht{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for export to Open XML format (aka default MS Office 200" +
    "7 format or .docx).")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_OpenXmlDocumentExporterOptions OpenXml{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for export to DOCM (Microsoft Office Open XML macro-enab" +
    "led document) format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_DocmDocumentExporterOptions Docm{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for export to DOTX (Microsoft Open Office XML macro-free" +
    " template) format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_DotxDocumentExporterOptions Dotx{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for export to DOTM (Microsoft Office Open XML macro-enab" +
    "led template) format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_DotmDocumentExporterOptions Dotm{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for export to FlatOpc (Microsoft Word XML Document, .xml" +
    ") format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_FlatOpcDocumentExporterOptions FlatOpc{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for export to FlatOpcTemplate (Microsoft Word XML Templa" +
    "te, .xml) format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_FlatOpcTemplateDocumentExporterOptions FlatOpcTemplate{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for export to FlatOpcMacroEnabled (Microsoft Word XML Ma" +
    "cro-Enabled Document, .xml) format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_FlatOpcMacroEnabledDocumentExporterOptions FlatOpcMacroEnabled{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for export to FlatOpcMacroEnabledTemplate (Microsoft Wor" +
    "d XML Macro-Enabled Template, .xml) format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_FlatOpcMacroEnabledTemplateDocumentExporterOptions FlatOpcMacroEnabledTemplate{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for export to OpenDocument text (.odt) format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_OpenDocumentExporterOptions OpenDocument{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for export to WordML (MS Office 2003 WordprocessingML) f" +
    "ormat.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_WordMLDocumentExporterOptions WordML{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for export to DOC (MS Word 97-2003) format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_DocDocumentExporterOptions Doc{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for export to DOT (Microsoft Word 97 - 2007 Template) fo" +
    "rmat.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_DotDocumentExporterOptions Dot{get;}

}


public interface IModelDevExpressXtraRichEditExport_RtfDocumentExporterCompatibilityOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether inline objects are saved in the RTF file twice - as an objec" +
    "t and as metafile content.")]
System.Boolean? DuplicateObjectAsMetafile{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies tags used to represent the BackColor attribute in a resulting RTF docum" +
    "ent.")]
DevExpress.XtraRichEdit.Export.RtfRunBackColorExportMode? BackColorExportMode{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_RtfDocumentExporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("For internal use.")]
System.Boolean? WrapContentInGroup{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the color theme information is included in the exported RTF " +
    "file.")]
System.Boolean? ExportTheme{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the way the lists are represented in the exported RTF document.")]
DevExpress.XtraRichEdit.Export.Rtf.RtfNumberingListExportFormat? ListExportFormat{get;set;}
[System.ComponentModel.DescriptionAttribute("Enables you to add the ‘\\par’ tag to the end of RTF content.")]
DevExpress.XtraRichEdit.Export.Rtf.ExportFinalParagraphMark? ExportFinalParagraphMark{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to options affecting the compatibility of exported files with dif" +
    "ferent RTF editors.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditExport_RtfDocumentExporterCompatibilityOptions Compatibility{get;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the document properties being exported.")]
DevExpress.XtraRichEdit.Export.DocumentPropertyNames? ExportedDocumentProperties{get;set;}

}


public interface IModelDevExpressXtraRichEditExport_WordMLDocumentExporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets the document properties being exported.")]
DevExpress.XtraRichEdit.Export.DocumentPropertyNames? ExportedDocumentProperties{get;set;}

}


public interface IModelDevExpressXtraRichEditImport_DocDocumentImporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to keep bookmarks contained in the range deleted with the Tr" +
    "ack Changes option.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? KeepBookmarksForRemovedRanges{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to keep the permissions applied to the ranges deleted with t" +
    "he Track Changes option.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? KeepPermissionsForRemovedRanges{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to keep the comment applied to the deleted ranges.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? KeepCommentsForRemovedRanges{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to options that specify whether a certain document field is updat" +
    "ed during import.")]
IModelDevExpressXtraRichEditImport_UpdateFieldOptions UpdateField{get;}

}


public interface IModelDevExpressXtraRichEditImport_DocmDocumentImporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("")]
System.Boolean? IgnoreParseErrors{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to options that specify whether a certain document field is updat" +
    "ed during import.")]
IModelDevExpressXtraRichEditImport_UpdateFieldOptions UpdateField{get;}

}


public interface IModelDevExpressXtraRichEditImport_DotDocumentImporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to keep bookmarks contained in the range deleted with the Tr" +
    "ack Changes option.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? KeepBookmarksForRemovedRanges{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to keep the permissions applied to the ranges deleted with t" +
    "he Track Changes option.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? KeepPermissionsForRemovedRanges{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to keep the comment applied to the deleted ranges.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? KeepCommentsForRemovedRanges{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to options that specify whether a certain document field is updat" +
    "ed during import.")]
IModelDevExpressXtraRichEditImport_UpdateFieldOptions UpdateField{get;}

}


public interface IModelDevExpressXtraRichEditImport_DotmDocumentImporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("")]
System.Boolean? IgnoreParseErrors{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to options that specify whether a certain document field is updat" +
    "ed during import.")]
IModelDevExpressXtraRichEditImport_UpdateFieldOptions UpdateField{get;}

}


public interface IModelDevExpressXtraRichEditImport_DotxDocumentImporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("")]
System.Boolean? IgnoreParseErrors{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to options that specify whether a certain document field is updat" +
    "ed during import.")]
IModelDevExpressXtraRichEditImport_UpdateFieldOptions UpdateField{get;}

}


public interface IModelDevExpressXtraRichEditImport_FlatOpcDocumentImporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("")]
System.Boolean? IgnoreParseErrors{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to options that specify whether a certain document field is updat" +
    "ed during import.")]
IModelDevExpressXtraRichEditImport_UpdateFieldOptions UpdateField{get;}

}


public interface IModelDevExpressXtraRichEditImport_FlatOpcMacroEnabledDocumentImporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("")]
System.Boolean? IgnoreParseErrors{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to options that specify whether a certain document field is updat" +
    "ed during import.")]
IModelDevExpressXtraRichEditImport_UpdateFieldOptions UpdateField{get;}

}


public interface IModelDevExpressXtraRichEditImport_FlatOpcMacroEnabledTemplateDocumentImporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("")]
System.Boolean? IgnoreParseErrors{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to options that specify whether a certain document field is updat" +
    "ed during import.")]
IModelDevExpressXtraRichEditImport_UpdateFieldOptions UpdateField{get;}

}


public interface IModelDevExpressXtraRichEditImport_FlatOpcTemplateDocumentImporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("")]
System.Boolean? IgnoreParseErrors{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to options that specify whether a certain document field is updat" +
    "ed during import.")]
IModelDevExpressXtraRichEditImport_UpdateFieldOptions UpdateField{get;}

}


public interface IModelDevExpressXtraRichEditImport_HtmlDocumentImporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to enable the auto-detection of the loaded text encoding.")]
System.Boolean? AutoDetectEncoding{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not to replace all white space characters inside the “pre” t" +
    "ag with non-breaking spaces.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? ReplaceSpaceWithNonBreakingSpaceInsidePre{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the character encoding specified in the META element should " +
    "be ignored.")]
System.Boolean? IgnoreMetaCharset{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not to ignore the “float” setting of HTML elements in the im" +
    "ported document.")]
System.Boolean? IgnoreFloatProperty{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to ignore media rules on HTML import.")]
System.Boolean? IgnoreMediaQueries{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether images are loaded synchronously or asynchronously when impor" +
    "ting an HTML document.")]
System.Boolean? AsyncImageLoading{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the DPI value that will be used to scale fonts on high DPI settings." +
    "")]
System.Int32? FontScalingDpiValue{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the DPI value used to scale images on high DPI settings.")]
System.Int32? ImageScalingDpi{get;set;}

}


public interface IModelDevExpressXtraRichEditImport_MhtDocumentImporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Overrides the corresponding property of the base class to hide it.")]
System.String SourceUri{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to enable the auto-detection of the loaded text encoding.")]
System.Boolean? AutoDetectEncoding{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not to replace all white space characters inside the “pre” t" +
    "ag with non-breaking spaces.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? ReplaceSpaceWithNonBreakingSpaceInsidePre{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the character encoding specified in the META element should " +
    "be ignored.")]
System.Boolean? IgnoreMetaCharset{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not to ignore the “float” setting of HTML elements in the im" +
    "ported document.")]
System.Boolean? IgnoreFloatProperty{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to ignore media rules on HTML import.")]
System.Boolean? IgnoreMediaQueries{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether images are loaded synchronously or asynchronously when impor" +
    "ting an HTML document.")]
System.Boolean? AsyncImageLoading{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the DPI value that will be used to scale fonts on high DPI settings." +
    "")]
System.Int32? FontScalingDpiValue{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the DPI value used to scale images on high DPI settings.")]
System.Int32? ImageScalingDpi{get;set;}

}


public interface IModelDevExpressXtraRichEditImport_OpenDocumentImporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Provides access to options that specify whether a certain document field is updat" +
    "ed during import.")]
IModelDevExpressXtraRichEditImport_UpdateFieldOptions UpdateField{get;}

}


public interface IModelDevExpressXtraRichEditImport_OpenXmlDocumentImporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("")]
System.Boolean? IgnoreParseErrors{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to options that specify whether a certain document field is updat" +
    "ed during import.")]
IModelDevExpressXtraRichEditImport_UpdateFieldOptions UpdateField{get;}

}


public interface IModelDevExpressXtraRichEditImport_PlainTextDocumentImporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether to enable the auto-detection of the loaded text encoding.")]
System.Boolean? AutoDetectEncoding{get;set;}

}


public interface IModelDevExpressXtraRichEditImport_RichEditDocumentImportOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Retrieves options specific for importing documents in RTF format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_RtfDocumentImporterOptions Rtf{get;}
[System.ComponentModel.DescriptionAttribute("Retrieves options specific for importing documents in plain text format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_PlainTextDocumentImporterOptions PlainText{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for importing documents in HTML format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_HtmlDocumentImporterOptions Html{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for importing documents in Mht (Web Archive) format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_MhtDocumentImporterOptions Mht{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for importing documents in OpenXml format (aka default M" +
    "S Office 2007 format or .docx).")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_OpenXmlDocumentImporterOptions OpenXml{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for importing documents in DOCM (Microsoft Office Open X" +
    "ML macro-enabled document) format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_DocmDocumentImporterOptions Docm{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for importing documents in DOTX (Microsoft Office Open X" +
    "ML macro-free template) format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_DotxDocumentImporterOptions Dotx{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for importing documents in DOTM format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_DotmDocumentImporterOptions Dotm{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options to import documents in FlatOpc (Microsoft Word XML Document, .xml" +
    ") format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_FlatOpcDocumentImporterOptions FlatOpc{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options to import documents in FlatOpcTemplate (Microsoft Word XML Templa" +
    "te, .xml) format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_FlatOpcTemplateDocumentImporterOptions FlatOpcTemplate{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options to import documents in FlatOpcMacroEnabled (Microsoft Word XML Ma" +
    "cro-Enabled Document, .xml) format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_FlatOpcMacroEnabledDocumentImporterOptions FlatOpcMacroEnabled{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options to import documents in FlatOpcMacroEnabledTemplate (Microsoft Wor" +
    "d XML Macro-Enabled Template, .xml) format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_FlatOpcMacroEnabledTemplateDocumentImporterOptions FlatOpcMacroEnabledTemplate{get;}
[System.ComponentModel.DescriptionAttribute("Retrieves options specific for importing documents in OpenDocument text (.odt) fo" +
    "rmat.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_OpenDocumentImporterOptions OpenDocument{get;}
[System.ComponentModel.DescriptionAttribute("Retrieves options specific for importing documents in WordML format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_WordMLDocumentImporterOptions WordML{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for importing documents in Microsoft Word binary file fo" +
    "rmat (MS Word 97 - 2003 .doc format).")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_DocDocumentImporterOptions Doc{get;}
[System.ComponentModel.DescriptionAttribute("Obtains options specific for importing documents in DOT (Microsoft Word 97 - 2007" +
    " Template) format.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
IModelDevExpressXtraRichEditImport_DotDocumentImporterOptions Dot{get;}
[System.ComponentModel.DescriptionAttribute("Gets or sets the format that is used if no distinct format can be specified or re" +
    "cognized.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
DevExpress.XtraRichEdit.DocumentFormat? FallbackFormat{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies a password used to unprotect a password-protected document during impor" +
    "t.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.String EncryptionPassword{get;set;}

}


public interface IModelDevExpressXtraRichEditImport_RtfDocumentImporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Provides access to options that specify whether a certain document field is updat" +
    "ed during import.")]
IModelDevExpressXtraRichEditImport_UpdateFieldOptions UpdateField{get;}

}


public interface IModelDevExpressXtraRichEditImport_UpdateFieldOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets whether the import of a document containing the DATE field updates t" +
    "his field.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? Date{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets whether import of a document containing the TIME field updates this " +
    "field.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? Time{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether to update the DOCVARIABLE field when the document is loaded.")]
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
System.Boolean? DocVariable{get;set;}

}


public interface IModelDevExpressXtraRichEditImport_WordMLDocumentImporterOptions:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.NotifyParentPropertyAttribute(true)]
[System.ComponentModel.DescriptionAttribute("")]
System.Boolean? IgnoreParseErrors{get;set;}
[System.ComponentModel.DescriptionAttribute("Provides access to options that specify whether a certain document field is updat" +
    "ed during import.")]
IModelDevExpressXtraRichEditImport_UpdateFieldOptions UpdateField{get;}

}


public interface IModelDevExpressXtraRichEditModel_DocumentModelAccessor:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{


}


public interface IModelSystem_Byte:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{


}

public interface IModelSystem_Bytes:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelSystem_Byte>{}


public interface IModelSystem_Guid:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{


}

public interface IModelSystem_Guids:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelSystem_Guid>{}


public interface IModelSystem_Int32:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{


}

public interface IModelSystem_Int32s:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelSystem_Int32>{}


public interface IModelSystem_Single:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{


}

public interface IModelSystem_Singles:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelSystem_Single>{}


public interface IModelSystem_String:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{


}

public interface IModelSystem_Strings:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelSystem_String>{}


public interface IModelSystem_Uri:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelSystem_Strings Segments{get;}

}

[System.ComponentModel.EditorAttribute("System.Drawing.Design.ColorEditor, System.Drawing.Design, Version=4.0.0.0, Cultur" +
    "e=neutral, PublicKeyToken=b03f5f7f11d50a3a","System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neut" +
    "ral, PublicKeyToken=b03f5f7f11d50a3a")]
[System.ComponentModel.TypeConverterAttribute("System.Drawing.ColorConverter, System.Drawing, Version=4.0.0.0, Culture=neutral, " +
    "PublicKeyToken=b03f5f7f11d50a3a")]
public interface IModelSystemDrawing_Color:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{


}

public interface IModelSystemDrawing_Colors:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelSystemDrawing_Color>{}

[System.ComponentModel.EditorAttribute("System.Drawing.Design.FontEditor, System.Drawing.Design, Version=4.0.0.0, Culture" +
    "=neutral, PublicKeyToken=b03f5f7f11d50a3a","System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neut" +
    "ral, PublicKeyToken=b03f5f7f11d50a3a")]
[System.ComponentModel.TypeConverterAttribute(typeof(System.Drawing.FontConverter))]
public interface IModelSystemDrawing_Font:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{


}


public interface IModelSystemDrawing_FontFamily:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelSystemDrawing_FontFamilys Families{get;}

}

public interface IModelSystemDrawing_FontFamilys:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelSystemDrawing_FontFamily>{}

[System.ComponentModel.EditorAttribute("System.Drawing.Design.ImageEditor, System.Drawing.Design, Version=4.0.0.0, Cultur" +
    "e=neutral, PublicKeyToken=b03f5f7f11d50a3a","System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neut" +
    "ral, PublicKeyToken=b03f5f7f11d50a3a")]
[System.ComponentModel.ImmutableObjectAttribute(true)]
[System.ComponentModel.TypeConverterAttribute(typeof(System.Drawing.ImageConverter))]
public interface IModelSystemDrawing_Image:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelSystemDrawingImaging_ImageFormat RawFormat{get;}

}

public interface IModelSystemDrawing_Images:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelSystemDrawing_Image>{}


public interface IModelSystemDrawing_Region:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{


}


public interface IModelSystemDrawing_StringFormat:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

System.Drawing.StringFormatFlags? FormatFlags{get;set;}

System.Drawing.StringAlignment? Alignment{get;set;}

System.Drawing.StringAlignment? LineAlignment{get;set;}

System.Drawing.Text.HotkeyPrefix? HotkeyPrefix{get;set;}

System.Drawing.StringTrimming? Trimming{get;set;}

}


public interface IModelSystemDrawingDrawing2D_Matrix:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelSystem_Singles Elements{get;}

}


public interface IModelSystemDrawingImaging_ColorPalette:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelSystemDrawing_Colors Entries{get;}

}

[System.ComponentModel.TypeConverterAttribute(typeof(System.Drawing.ImageFormatConverter))]
public interface IModelSystemDrawingImaging_ImageFormat:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{


}


public interface IModelSystemDrawingImaging_PropertyItem:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

System.Int32? Len{get;set;}

System.Int16? Type{get;set;}

IModelSystem_Bytes Value{get;}

}

public interface IModelSystemDrawingImaging_PropertyItems:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelSystemDrawingImaging_PropertyItem>{}


public interface IModelSystemGlobalization_Calendar:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelSystem_Int32s Eras{get;}

System.Int32? TwoDigitYearMax{get;set;}

}

public interface IModelSystemGlobalization_Calendars:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelSystemGlobalization_Calendar>{}


public interface IModelSystemGlobalization_CompareInfo:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{


}


public interface IModelSystemGlobalization_CultureInfo:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelSystemGlobalization_CompareInfo CompareInfo{get;}

IModelSystemGlobalization_TextInfo TextInfo{get;}

IModelSystemGlobalization_NumberFormatInfo NumberFormat{get;}

IModelSystemGlobalization_DateTimeFormatInfo DateTimeFormat{get;}

IModelSystemGlobalization_Calendars OptionalCalendars{get;}

}


public interface IModelSystemGlobalization_DateTimeFormatInfo:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

System.String AMDesignator{get;set;}

System.String DateSeparator{get;set;}

System.DayOfWeek? FirstDayOfWeek{get;set;}

System.Globalization.CalendarWeekRule? CalendarWeekRule{get;set;}

System.String FullDateTimePattern{get;set;}

System.String LongDatePattern{get;set;}

System.String LongTimePattern{get;set;}

System.String MonthDayPattern{get;set;}

System.String PMDesignator{get;set;}

System.String ShortDatePattern{get;set;}

System.String ShortTimePattern{get;set;}

System.String TimeSeparator{get;set;}

System.String YearMonthPattern{get;set;}

IModelSystem_Strings AbbreviatedDayNames{get;}

IModelSystem_Strings ShortestDayNames{get;}

IModelSystem_Strings DayNames{get;}

IModelSystem_Strings AbbreviatedMonthNames{get;}

IModelSystem_Strings MonthNames{get;}

IModelSystem_Strings AbbreviatedMonthGenitiveNames{get;}

IModelSystem_Strings MonthGenitiveNames{get;}

}


public interface IModelSystemGlobalization_NumberFormatInfo:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

System.Int32? CurrencyDecimalDigits{get;set;}

System.String CurrencyDecimalSeparator{get;set;}

IModelSystem_Int32s CurrencyGroupSizes{get;}

IModelSystem_Int32s NumberGroupSizes{get;}

IModelSystem_Int32s PercentGroupSizes{get;}

System.String CurrencyGroupSeparator{get;set;}

System.String CurrencySymbol{get;set;}

System.String NaNSymbol{get;set;}

System.Int32? CurrencyNegativePattern{get;set;}

System.Int32? NumberNegativePattern{get;set;}

System.Int32? PercentPositivePattern{get;set;}

System.Int32? PercentNegativePattern{get;set;}

System.String NegativeInfinitySymbol{get;set;}

System.String NegativeSign{get;set;}

System.Int32? NumberDecimalDigits{get;set;}

System.String NumberDecimalSeparator{get;set;}

System.String NumberGroupSeparator{get;set;}

System.Int32? CurrencyPositivePattern{get;set;}

System.String PositiveInfinitySymbol{get;set;}

System.String PositiveSign{get;set;}

System.Int32? PercentDecimalDigits{get;set;}

System.String PercentDecimalSeparator{get;set;}

System.String PercentGroupSeparator{get;set;}

System.String PercentSymbol{get;set;}

System.String PerMilleSymbol{get;set;}

IModelSystem_Strings NativeDigits{get;}

System.Globalization.DigitShapes? DigitSubstitution{get;set;}

}


public interface IModelSystemGlobalization_TextInfo:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

System.String ListSeparator{get;set;}

}


public interface IModelSystemWindowsForms_AccessibleObject:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

System.String Name{get;set;}

System.String Value{get;set;}

}

[System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.Forms.ListBindingConverter))]
public interface IModelSystemWindowsForms_Binding:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

IModelSystemWindowsForms_Control Control{get;}

System.Boolean? IsBinding{get;set;}

System.Boolean? FormattingEnabled{get;set;}

System.String FormatString{get;set;}

System.Windows.Forms.ControlUpdateMode? ControlUpdateMode{get;set;}

System.Windows.Forms.DataSourceUpdateMode? DataSourceUpdateMode{get;set;}

}

public interface IModelSystemWindowsForms_Bindings:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelSystemWindowsForms_Binding>{}


public interface IModelSystemWindowsForms_ContextMenuStrip:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

System.Windows.Forms.ToolStripLayoutStyle? LayoutStyle{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether the image margin will be shown.")]
System.Boolean? ShowImageMargin{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether the check margin will be shown.")]
System.Boolean? ShowCheckMargin{get;set;}

System.Boolean? AutoSize{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether the DropDown automatically closes through user action.")]
System.Boolean? AutoClose{get;set;}

System.Windows.Forms.ToolStripDropDownDirection? DefaultDropDownDirection{get;set;}

System.Boolean? DropShadowEnabled{get;set;}

IModelSystemDrawing_Font Font{get;}
[System.ComponentModel.AmbientValueAttribute(System.Windows.Forms.RightToLeft.Inherit)]
[System.ComponentModel.DescriptionAttribute("Indicates whether the component should draw right-to-left for RTL languages.")]
System.Windows.Forms.RightToLeft? RightToLeft{get;set;}

System.Boolean? AllowDrop{get;set;}
[System.ComponentModel.DescriptionAttribute("Allow the items to be merged.")]
System.Boolean? AllowMerge{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the background color of the ToolStrip.")]
System.Drawing.Color? BackColor{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the size of images on items.  To control the scaling of items, use the " +
    "\'ToolStripItem.ImageScaling\' property.")]
System.Drawing.Size? ImageScalingSize{get;set;}
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
[System.ComponentModel.MergablePropertyAttribute(false)]
[System.ComponentModel.DescriptionAttribute("Collection of items to display on the ToolStrip.")]
IModelSystem_Strings Items{get;}
[System.ComponentModel.DescriptionAttribute("The painting styles applied to the control.")]
System.Windows.Forms.ToolStripRenderMode? RenderMode{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether to display ToolTips on items.")]
System.Boolean? ShowItemToolTips{get;set;}
[System.Runtime.InteropServices.DispIdAttribute(-516)]
[System.ComponentModel.DescriptionAttribute("Indicates whether the user can use the TAB key to give focus to the control.")]
System.Boolean? TabStop{get;set;}
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The description that will be reported to accessibility clients.")]
System.String AccessibleDescription{get;set;}
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The name that will be reported to accessibility clients.")]
System.String AccessibleName{get;set;}
[System.ComponentModel.DescriptionAttribute("The role that will be reported to accessibility clients.")]
System.Windows.Forms.AccessibleRole? AccessibleRole{get;set;}
[System.ComponentModel.DescriptionAttribute("The background image layout used for the component.")]
System.Windows.Forms.ImageLayout? BackgroundImageLayout{get;set;}
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[System.ComponentModel.ParenthesizePropertyNameAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The data bindings for the control.")]
IModelSystemWindowsForms_Bindings DataBindings{get;}

IModelSystemDrawing_Font DefaultFont{get;}
[System.Runtime.InteropServices.DispIdAttribute(-514)]
[System.ComponentModel.DescriptionAttribute("Indicates whether the control is enabled.")]
System.Boolean? Enabled{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies space between this control and another control\'s margin.")]
System.Windows.Forms.Padding? Margin{get;set;}
[System.ComponentModel.AmbientValueAttribute(typeof(System.Drawing.Size),"0, 0")]
[System.ComponentModel.DescriptionAttribute("Specifies the maximum size of the control.")]
System.Drawing.Size? MaximumSize{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the minimum size of the control.")]
System.Drawing.Size? MinimumSize{get;set;}
[System.ComponentModel.DescriptionAttribute("The size of the control in pixels.")]
System.Drawing.Size? Size{get;set;}
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.BindableAttribute(true)]
[System.Runtime.InteropServices.DispIdAttribute(-517)]
[System.ComponentModel.DescriptionAttribute("The text associated with the control.")]
System.String Text{get;set;}
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
[System.ComponentModel.BrowsableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("When this property is true, the Cursor property of the control and its child cont" +
    "rols is set to WaitCursor.")]
System.Boolean? UseWaitCursor{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the interior spacing of a control.")]
System.Windows.Forms.Padding? Padding{get;set;}
[System.ComponentModel.AmbientValueAttribute(System.Windows.Forms.ImeMode.Inherit)]
[System.ComponentModel.DescriptionAttribute("Determines the IME (Input Method Editor) status of the object when selected.")]
System.Windows.Forms.ImeMode? ImeMode{get;set;}

}


public interface IModelSystemWindowsForms_Control:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The description that will be reported to accessibility clients.")]
System.String AccessibleDescription{get;set;}
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The name that will be reported to accessibility clients.")]
System.String AccessibleName{get;set;}
[System.ComponentModel.DescriptionAttribute("The role that will be reported to accessibility clients.")]
System.Windows.Forms.AccessibleRole? AccessibleRole{get;set;}
[System.ComponentModel.DescriptionAttribute("Indicates whether the control can accept data that the user drags onto it.")]
System.Boolean? AllowDrop{get;set;}
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.Repaint)]
[System.ComponentModel.DescriptionAttribute("Defines the edges of the container to which a certain control is bound. When a co" +
    "ntrol is anchored to an edge, the distance between the control\'s closest edge an" +
    "d the specified edge will remain constant. ")]
System.Windows.Forms.AnchorStyles? Anchor{get;set;}
[System.Runtime.InteropServices.DispIdAttribute(-501)]
[System.ComponentModel.DescriptionAttribute("The background color of the component. ")]
System.Drawing.Color? BackColor{get;set;}
[System.ComponentModel.DescriptionAttribute("The background image layout used for the component.")]
System.Windows.Forms.ImageLayout? BackgroundImageLayout{get;set;}
[System.ComponentModel.DescriptionAttribute("Indicates whether this component raises validation events. ")]
System.Boolean? CausesValidation{get;set;}
[System.ComponentModel.DescriptionAttribute("The shortcut menu to display when the user right-clicks the control.")]
IModelSystemWindowsForms_ContextMenuStrip ContextMenuStrip{get;}
[System.ComponentModel.AmbientValueAttribute(null)]
[System.ComponentModel.DescriptionAttribute("The cursor that appears when the pointer moves over the control.")]
IModelSystemWindowsForms_Cursor Cursor{get;}
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[System.ComponentModel.ParenthesizePropertyNameAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The data bindings for the control.")]
IModelSystemWindowsForms_Bindings DataBindings{get;}

IModelSystemDrawing_Font DefaultFont{get;}
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.Repaint)]
[System.ComponentModel.DescriptionAttribute("Defines which borders of the control are bound to the container. ")]
System.Windows.Forms.DockStyle? Dock{get;set;}
[System.Runtime.InteropServices.DispIdAttribute(-514)]
[System.ComponentModel.DescriptionAttribute("Indicates whether the control is enabled.")]
System.Boolean? Enabled{get;set;}
[System.Runtime.InteropServices.DispIdAttribute(-512)]
[System.ComponentModel.AmbientValueAttribute(null)]
[System.ComponentModel.DescriptionAttribute("The font used to display text in the control.")]
IModelSystemDrawing_Font Font{get;}
[System.Runtime.InteropServices.DispIdAttribute(-513)]
[System.ComponentModel.DescriptionAttribute("The foreground color of this component, which is used to display text.")]
System.Drawing.Color? ForeColor{get;set;}
[System.ComponentModel.DescriptionAttribute("The coordinates of the upper-left corner of the control relative to the upper-lef" +
    "t corner of its container.")]
System.Drawing.Point? Location{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies space between this control and another control\'s margin.")]
System.Windows.Forms.Padding? Margin{get;set;}
[System.ComponentModel.AmbientValueAttribute(typeof(System.Drawing.Size),"0, 0")]
[System.ComponentModel.DescriptionAttribute("Specifies the maximum size of the control.")]
System.Drawing.Size? MaximumSize{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the minimum size of the control.")]
System.Drawing.Size? MinimumSize{get;set;}
[System.ComponentModel.AmbientValueAttribute(System.Windows.Forms.RightToLeft.Inherit)]
[System.ComponentModel.DescriptionAttribute("Indicates whether the component should draw right-to-left for RTL languages.")]
System.Windows.Forms.RightToLeft? RightToLeft{get;set;}
[System.ComponentModel.DescriptionAttribute("The size of the control in pixels.")]
System.Drawing.Size? Size{get;set;}
[System.ComponentModel.MergablePropertyAttribute(false)]
[System.ComponentModel.DescriptionAttribute("Determines the index in the TAB order that this control will occupy.")]
System.Int32? TabIndex{get;set;}
[System.Runtime.InteropServices.DispIdAttribute(-516)]
[System.ComponentModel.DescriptionAttribute("Indicates whether the user can use the TAB key to give focus to the control.")]
System.Boolean? TabStop{get;set;}
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.BindableAttribute(true)]
[System.Runtime.InteropServices.DispIdAttribute(-517)]
[System.ComponentModel.DescriptionAttribute("The text associated with the control.")]
System.String Text{get;set;}
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
[System.ComponentModel.BrowsableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("When this property is true, the Cursor property of the control and its child cont" +
    "rols is set to WaitCursor.")]
System.Boolean? UseWaitCursor{get;set;}
[System.ComponentModel.DescriptionAttribute("Determines whether the control is visible or hidden.")]
System.Boolean? Visible{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the interior spacing of a control.")]
System.Windows.Forms.Padding? Padding{get;set;}
[System.ComponentModel.AmbientValueAttribute(System.Windows.Forms.ImeMode.Inherit)]
[System.ComponentModel.DescriptionAttribute("Determines the IME (Input Method Editor) status of the object when selected.")]
System.Windows.Forms.ImeMode? ImeMode{get;set;}

}

public interface IModelSystemWindowsForms_Controls:DevExpress.ExpressApp.Model.IModelNode,DevExpress.ExpressApp.Model.IModelList<IModelSystemWindowsForms_Control>{}

[System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.Forms.CursorConverter))]
[System.ComponentModel.EditorAttribute("System.Drawing.Design.CursorEditor, System.Drawing.Design, Version=4.0.0.0, Cultu" +
    "re=neutral, PublicKeyToken=b03f5f7f11d50a3a",typeof(System.Drawing.Design.UITypeEditor))]
public interface IModelSystemWindowsForms_Cursor:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

System.Drawing.Rectangle? Clip{get;set;}

System.Drawing.Point? Position{get;set;}

}

[System.ComponentModel.TypeConverterAttribute(typeof(System.Windows.Forms.ScrollableControl.DockPaddingEdgesConverter))]
public interface IModelSystemWindowsForms_DockPaddingEdges:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[System.ComponentModel.DescriptionAttribute("Number of pixels along all borders to pad docked controls.")]
System.Int32? All{get;set;}
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[System.ComponentModel.DescriptionAttribute("Number of pixels along the bottom border to pad docked controls.")]
System.Int32? Bottom{get;set;}
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[System.ComponentModel.DescriptionAttribute("Number of pixels along the left border to pad docked controls.")]
System.Int32? Left{get;set;}
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[System.ComponentModel.DescriptionAttribute("Number of pixels along the right border to pad docked controls.")]
System.Int32? Right{get;set;}
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[System.ComponentModel.DescriptionAttribute("Number of pixels along the top border to pad docked controls.")]
System.Int32? Top{get;set;}

}


public interface IModelSystemWindowsForms_HScrollProperties:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets a Boolean value controlling whether the scrollbar is enabled.")]
System.Boolean? Enabled{get;set;}
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.Repaint)]
[System.ComponentModel.DescriptionAttribute("The amount by which the scroll box position changes when the user clicks in the s" +
    "croll bar or presses the PAGE UP or PAGE DOWN keys.")]
System.Int32? LargeChange{get;set;}
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.Repaint)]
[System.ComponentModel.DescriptionAttribute("The upper limit value of the scrollable range. ")]
System.Int32? Maximum{get;set;}
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.Repaint)]
[System.ComponentModel.DescriptionAttribute("The lower limit value of the scrollable range. ")]
System.Int32? Minimum{get;set;}
[System.ComponentModel.DescriptionAttribute("The amount by which the scroll box position changes when the user clicks a scroll" +
    " arrow or presses an arrow key.")]
System.Int32? SmallChange{get;set;}
[System.ComponentModel.BindableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The value that the scroll box position represents. ")]
System.Int32? Value{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a Boolean value controlling whether the scrollbar is showing.")]
System.Boolean? Visible{get;set;}

}


public interface IModelSystemWindowsForms_ImageList:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("The number of colors to use to render images.")]
System.Windows.Forms.ColorDepth? ColorDepth{get;set;}
[System.ComponentModel.DescriptionAttribute("The size of individual images in the ImageList.")]
System.Drawing.Size? ImageSize{get;set;}
[System.ComponentModel.DescriptionAttribute("The color that is treated as transparent.")]
System.Drawing.Color? TransparentColor{get;set;}

}


public interface IModelSystemWindowsForms_ImageListStreamer:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{


}


public interface IModelSystemWindowsForms_ToolStrip:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.BrowsableAttribute(true)]
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Visible)]
System.Boolean? AutoSize{get;set;}

System.Boolean? AllowDrop{get;set;}
[System.ComponentModel.DescriptionAttribute("Allows the items to be reordered when the ALT key is pressed.")]
System.Boolean? AllowItemReorder{get;set;}
[System.ComponentModel.DescriptionAttribute("Allow the items to be merged.")]
System.Boolean? AllowMerge{get;set;}

System.Windows.Forms.AnchorStyles? Anchor{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the background color of the ToolStrip.")]
System.Drawing.Color? BackColor{get;set;}
[System.ComponentModel.DescriptionAttribute("Indicates whether items can be sent to an overflow menu.")]
System.Boolean? CanOverflow{get;set;}

IModelSystemDrawing_Font Font{get;}

System.Windows.Forms.DockStyle? Dock{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies visibility of the grip on the ToolStrip.")]
System.Windows.Forms.ToolStripGripStyle? GripStyle{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the orientation of the grip on the ToolStrip.")]
System.Windows.Forms.Padding? GripMargin{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the size of images on items.  To control the scaling of items, use the " +
    "\'ToolStripItem.ImageScaling\' property.")]
System.Drawing.Size? ImageScalingSize{get;set;}
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
[System.ComponentModel.MergablePropertyAttribute(false)]
[System.ComponentModel.DescriptionAttribute("Collection of items to display on the ToolStrip.")]
IModelSystem_Strings Items{get;}
[System.ComponentModel.AmbientValueAttribute(System.Windows.Forms.ToolStripLayoutStyle.StackWithOverflow)]
[System.ComponentModel.DescriptionAttribute("Specifies the layout orientation of the ToolStrip.")]
System.Windows.Forms.ToolStripLayoutStyle? LayoutStyle{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether the ToolStrip stretches from end to end in the rafting containe" +
    "r.")]
System.Boolean? Stretch{get;set;}
[System.ComponentModel.DescriptionAttribute("The painting styles applied to the control.")]
System.Windows.Forms.ToolStripRenderMode? RenderMode{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether to display ToolTips on items.")]
System.Boolean? ShowItemToolTips{get;set;}
[System.Runtime.InteropServices.DispIdAttribute(-516)]
[System.ComponentModel.DescriptionAttribute("Indicates whether the user can use the TAB key to give focus to the control.")]
System.Boolean? TabStop{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the direction to draw the text on the item.")]
System.Windows.Forms.ToolStripTextDirection? TextDirection{get;set;}
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The description that will be reported to accessibility clients.")]
System.String AccessibleDescription{get;set;}
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The name that will be reported to accessibility clients.")]
System.String AccessibleName{get;set;}
[System.ComponentModel.DescriptionAttribute("The role that will be reported to accessibility clients.")]
System.Windows.Forms.AccessibleRole? AccessibleRole{get;set;}
[System.ComponentModel.DescriptionAttribute("The background image layout used for the component.")]
System.Windows.Forms.ImageLayout? BackgroundImageLayout{get;set;}
[System.ComponentModel.DescriptionAttribute("The shortcut menu to display when the user right-clicks the control.")]
IModelSystemWindowsForms_ContextMenuStrip ContextMenuStrip{get;}
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[System.ComponentModel.ParenthesizePropertyNameAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The data bindings for the control.")]
IModelSystemWindowsForms_Bindings DataBindings{get;}

IModelSystemDrawing_Font DefaultFont{get;}
[System.Runtime.InteropServices.DispIdAttribute(-514)]
[System.ComponentModel.DescriptionAttribute("Indicates whether the control is enabled.")]
System.Boolean? Enabled{get;set;}
[System.ComponentModel.DescriptionAttribute("The coordinates of the upper-left corner of the control relative to the upper-lef" +
    "t corner of its container.")]
System.Drawing.Point? Location{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies space between this control and another control\'s margin.")]
System.Windows.Forms.Padding? Margin{get;set;}
[System.ComponentModel.AmbientValueAttribute(typeof(System.Drawing.Size),"0, 0")]
[System.ComponentModel.DescriptionAttribute("Specifies the maximum size of the control.")]
System.Drawing.Size? MaximumSize{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the minimum size of the control.")]
System.Drawing.Size? MinimumSize{get;set;}
[System.ComponentModel.AmbientValueAttribute(System.Windows.Forms.RightToLeft.Inherit)]
[System.ComponentModel.DescriptionAttribute("Indicates whether the component should draw right-to-left for RTL languages.")]
System.Windows.Forms.RightToLeft? RightToLeft{get;set;}
[System.ComponentModel.DescriptionAttribute("The size of the control in pixels.")]
System.Drawing.Size? Size{get;set;}
[System.ComponentModel.MergablePropertyAttribute(false)]
[System.ComponentModel.DescriptionAttribute("Determines the index in the TAB order that this control will occupy.")]
System.Int32? TabIndex{get;set;}
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.BindableAttribute(true)]
[System.Runtime.InteropServices.DispIdAttribute(-517)]
[System.ComponentModel.DescriptionAttribute("The text associated with the control.")]
System.String Text{get;set;}
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
[System.ComponentModel.BrowsableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("When this property is true, the Cursor property of the control and its child cont" +
    "rols is set to WaitCursor.")]
System.Boolean? UseWaitCursor{get;set;}
[System.ComponentModel.DescriptionAttribute("Determines whether the control is visible or hidden.")]
System.Boolean? Visible{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the interior spacing of a control.")]
System.Windows.Forms.Padding? Padding{get;set;}
[System.ComponentModel.AmbientValueAttribute(System.Windows.Forms.ImeMode.Inherit)]
[System.ComponentModel.DescriptionAttribute("Determines the IME (Input Method Editor) status of the object when selected.")]
System.Windows.Forms.ImeMode? ImeMode{get;set;}

}


public interface IModelSystemWindowsForms_ToolStripDropDown:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

System.Boolean? AutoSize{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether the DropDown automatically closes through user action.")]
System.Boolean? AutoClose{get;set;}

System.Windows.Forms.ToolStripDropDownDirection? DefaultDropDownDirection{get;set;}

System.Boolean? DropShadowEnabled{get;set;}

IModelSystemDrawing_Font Font{get;}
[System.ComponentModel.AmbientValueAttribute(System.Windows.Forms.RightToLeft.Inherit)]
[System.ComponentModel.DescriptionAttribute("Indicates whether the component should draw right-to-left for RTL languages.")]
System.Windows.Forms.RightToLeft? RightToLeft{get;set;}

System.Boolean? AllowDrop{get;set;}
[System.ComponentModel.DescriptionAttribute("Allow the items to be merged.")]
System.Boolean? AllowMerge{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the background color of the ToolStrip.")]
System.Drawing.Color? BackColor{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the size of images on items.  To control the scaling of items, use the " +
    "\'ToolStripItem.ImageScaling\' property.")]
System.Drawing.Size? ImageScalingSize{get;set;}
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
[System.ComponentModel.MergablePropertyAttribute(false)]
[System.ComponentModel.DescriptionAttribute("Collection of items to display on the ToolStrip.")]
IModelSystem_Strings Items{get;}
[System.ComponentModel.AmbientValueAttribute(System.Windows.Forms.ToolStripLayoutStyle.StackWithOverflow)]
[System.ComponentModel.DescriptionAttribute("Specifies the layout orientation of the ToolStrip.")]
System.Windows.Forms.ToolStripLayoutStyle? LayoutStyle{get;set;}
[System.ComponentModel.DescriptionAttribute("The painting styles applied to the control.")]
System.Windows.Forms.ToolStripRenderMode? RenderMode{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether to display ToolTips on items.")]
System.Boolean? ShowItemToolTips{get;set;}
[System.Runtime.InteropServices.DispIdAttribute(-516)]
[System.ComponentModel.DescriptionAttribute("Indicates whether the user can use the TAB key to give focus to the control.")]
System.Boolean? TabStop{get;set;}
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The description that will be reported to accessibility clients.")]
System.String AccessibleDescription{get;set;}
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The name that will be reported to accessibility clients.")]
System.String AccessibleName{get;set;}
[System.ComponentModel.DescriptionAttribute("The role that will be reported to accessibility clients.")]
System.Windows.Forms.AccessibleRole? AccessibleRole{get;set;}
[System.ComponentModel.DescriptionAttribute("The background image layout used for the component.")]
System.Windows.Forms.ImageLayout? BackgroundImageLayout{get;set;}
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[System.ComponentModel.ParenthesizePropertyNameAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The data bindings for the control.")]
IModelSystemWindowsForms_Bindings DataBindings{get;}

IModelSystemDrawing_Font DefaultFont{get;}
[System.Runtime.InteropServices.DispIdAttribute(-514)]
[System.ComponentModel.DescriptionAttribute("Indicates whether the control is enabled.")]
System.Boolean? Enabled{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies space between this control and another control\'s margin.")]
System.Windows.Forms.Padding? Margin{get;set;}
[System.ComponentModel.AmbientValueAttribute(typeof(System.Drawing.Size),"0, 0")]
[System.ComponentModel.DescriptionAttribute("Specifies the maximum size of the control.")]
System.Drawing.Size? MaximumSize{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the minimum size of the control.")]
System.Drawing.Size? MinimumSize{get;set;}
[System.ComponentModel.DescriptionAttribute("The size of the control in pixels.")]
System.Drawing.Size? Size{get;set;}
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.BindableAttribute(true)]
[System.Runtime.InteropServices.DispIdAttribute(-517)]
[System.ComponentModel.DescriptionAttribute("The text associated with the control.")]
System.String Text{get;set;}
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
[System.ComponentModel.BrowsableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("When this property is true, the Cursor property of the control and its child cont" +
    "rols is set to WaitCursor.")]
System.Boolean? UseWaitCursor{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the interior spacing of a control.")]
System.Windows.Forms.Padding? Padding{get;set;}
[System.ComponentModel.AmbientValueAttribute(System.Windows.Forms.ImeMode.Inherit)]
[System.ComponentModel.DescriptionAttribute("Determines the IME (Input Method Editor) status of the object when selected.")]
System.Windows.Forms.ImeMode? ImeMode{get;set;}

}


public interface IModelSystemWindowsForms_ToolStripOverflowButton:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{

System.Boolean? AutoToolTip{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether or not an arrow should be shown on the drop down button.")]
System.Boolean? ShowDropDownArrow{get;set;}
[System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ReferenceConverter))]
[System.ComponentModel.DescriptionAttribute("Specifies a ToolStripDropDown to show when the item is clicked.")]
IModelSystemWindowsForms_ToolStripDropDown DropDown{get;}
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
[System.ComponentModel.DescriptionAttribute("Specifies a ToolStripItem to display when the item is clicked.")]
IModelSystem_Strings DropDownItems{get;}
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The description that will be reported to accessibility clients.")]
System.String AccessibleDescription{get;set;}
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The name that will be reported to accessibility clients.")]
System.String AccessibleName{get;set;}
[System.ComponentModel.DescriptionAttribute("The role that will be reported to accessibility clients.")]
System.Windows.Forms.AccessibleRole? AccessibleRole{get;set;}
[System.ComponentModel.DescriptionAttribute("Indicates whether the item aligns toward the beginning or end of the ToolStrip.")]
System.Windows.Forms.ToolStripItemAlignment? Alignment{get;set;}
[System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Visible)]
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
[System.ComponentModel.DescriptionAttribute("Determines whether the item should automatically size based on its image and text" +
    ".")]
System.Boolean? AutoSize{get;set;}
[System.ComponentModel.DescriptionAttribute("The background image layout used for the component.")]
System.Windows.Forms.ImageLayout? BackgroundImageLayout{get;set;}
[System.ComponentModel.DescriptionAttribute("The background color used to display text and graphics in the control.")]
System.Drawing.Color? BackColor{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether the image and text are rendered.")]
System.Windows.Forms.ToolStripItemDisplayStyle? DisplayStyle{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether the DoubleClick event will occur.")]
System.Boolean? DoubleClickEnabled{get;set;}
[System.ComponentModel.DescriptionAttribute("Indicates whether the control is enabled.")]
System.Boolean? Enabled{get;set;}
[System.ComponentModel.DescriptionAttribute("The foreground color used to display text and graphics in the item.")]
System.Drawing.Color? ForeColor{get;set;}
[System.ComponentModel.DescriptionAttribute("The font used to display text in the item.")]
IModelSystemDrawing_Font Font{get;}
[System.ComponentModel.DescriptionAttribute("The alignment of the image that will be displayed on the item.")]
System.Drawing.ContentAlignment? ImageAlign{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the transparent color on the item\'s image for images that support trans" +
    "parency.")]
System.Drawing.Color? ImageTransparentColor{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether the image on the item will size to fit on the ToolStrip.  To co" +
    "ntrol the image size, use the \'ToolStrip.ImageScalingSize\' property.")]
System.Windows.Forms.ToolStripItemImageScaling? ImageScaling{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the spacing between this item and an adjacent item.")]
System.Windows.Forms.Padding? Margin{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies what action to take if match is successful.")]
System.Windows.Forms.MergeAction? MergeAction{get;set;}
[System.ComponentModel.DescriptionAttribute("Used for matching and positioning within target ToolStrip.")]
System.Int32? MergeIndex{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies whether the item will always move to the overflow, move to the overflow" +
    " as needed, or never move to the overflow.")]
System.Windows.Forms.ToolStripItemOverflow? Overflow{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the internal spacing within this item.")]
System.Windows.Forms.Padding? Padding{get;set;}
[System.ComponentModel.DescriptionAttribute("Indicates whether the item should draw right-to-left for RTL languages.")]
System.Windows.Forms.RightToLeft? RightToLeft{get;set;}
[System.ComponentModel.DescriptionAttribute("The size of the item in pixels.")]
System.Drawing.Size? Size{get;set;}
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The text to display on the item.")]
System.String Text{get;set;}
[System.ComponentModel.DescriptionAttribute("The alignment of the text that will be displayed on the item.")]
System.Drawing.ContentAlignment? TextAlign{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the direction to draw the text on the item.")]
System.Windows.Forms.ToolStripTextDirection? TextDirection{get;set;}
[System.ComponentModel.DescriptionAttribute("Specifies the relative location of the image to the text on the item.")]
System.Windows.Forms.TextImageRelation? TextImageRelation{get;set;}
[System.ComponentModel.EditorAttribute("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=4.0.0." +
    "0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",typeof(System.Drawing.Design.UITypeEditor))]
[System.ComponentModel.LocalizableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("Specifies the text to show on the ToolTip.")]
System.String ToolTipText{get;set;}
[System.ComponentModel.DescriptionAttribute("Determines whether the item is visible or hidden.")]
System.Boolean? Visible{get;set;}

}


public interface IModelSystemWindowsForms_VScrollProperties:Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled{
[System.ComponentModel.DescriptionAttribute("Gets or sets a Boolean value controlling whether the scrollbar is enabled.")]
System.Boolean? Enabled{get;set;}
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.Repaint)]
[System.ComponentModel.DescriptionAttribute("The amount by which the scroll box position changes when the user clicks in the s" +
    "croll bar or presses the PAGE UP or PAGE DOWN keys.")]
System.Int32? LargeChange{get;set;}
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.Repaint)]
[System.ComponentModel.DescriptionAttribute("The upper limit value of the scrollable range. ")]
System.Int32? Maximum{get;set;}
[System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.Repaint)]
[System.ComponentModel.DescriptionAttribute("The lower limit value of the scrollable range. ")]
System.Int32? Minimum{get;set;}
[System.ComponentModel.DescriptionAttribute("The amount by which the scroll box position changes when the user clicks a scroll" +
    " arrow or presses an arrow key.")]
System.Int32? SmallChange{get;set;}
[System.ComponentModel.BindableAttribute(true)]
[System.ComponentModel.DescriptionAttribute("The value that the scroll box position represents. ")]
System.Int32? Value{get;set;}
[System.ComponentModel.DescriptionAttribute("Gets or sets a Boolean value controlling whether the scrollbar is showing.")]
System.Boolean? Visible{get;set;}

}