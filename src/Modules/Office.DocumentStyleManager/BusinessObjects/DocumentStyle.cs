using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.XtraRichEdit.API.Native;
using DevExpress.XtraRichEdit.Model;

using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using ParagraphAlignment = DevExpress.XtraRichEdit.API.Native.ParagraphAlignment;
using ParagraphBorders = DevExpress.XtraRichEdit.API.Native.ParagraphBorders;
using ParagraphFirstLineIndent = DevExpress.XtraRichEdit.API.Native.ParagraphFirstLineIndent;
using ParagraphLineSpacing = DevExpress.XtraRichEdit.API.Native.ParagraphLineSpacing;
using StrikeoutType = DevExpress.XtraRichEdit.API.Native.StrikeoutType;
using UnderlineType = DevExpress.XtraRichEdit.API.Native.UnderlineType;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects{
    
    [SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
    public interface IDocumentStyle:CharacterPropertiesBase,ParagraphPropertiesBase{
        new Color? BackColor{ get; set; }
        DocumentStyleType DocumentStyleType{ get; set; }
        string StyleName{ get; set; }
        
        bool Used{ get; set; }
        
        bool IsDeleted{ get; set; }
        
        bool IsDefault{ get; set; }
        
        IDocumentStyle Parent{ get; set; }
        IDocumentStyle Next{ get; set; }
        Dictionary<string, string> PropeprtiesMap{ get; } 
    }

    public enum DocumentStyleType{
        Paragraph,
        Character
    }


    [DomainComponent]
    [Appearance("Color Used",AppearanceItemType.ViewItem, nameof(Used)+"=1",TargetItems = "*",FontColor = "DarkGreen")]
    [Appearance("Color Deleted",AppearanceItemType.ViewItem, nameof(IsDeleted)+"=1",TargetItems = "*",FontColor="LightGray" )]
    public class DocumentStyle: IDocumentStyle,INotifyPropertyChanged{
        private DocumentStyleType _documentStyleType;
        private string _styleName;
        private UnderlineType? _underline;
        private ParagraphAlignment? _alignment;
        private bool? _rightToLeft;
        private float? _leftIndent;
        private float? _rightIndent;
        private float? _spacingBefore;
        private float? _spacingAfter;
        private ParagraphLineSpacing? _lineSpacingType;
        private float? _lineSpacing;
        private float? _lineSpacingMultiplier;
        private ParagraphFirstLineIndent? _firstLineIndentType;
        private float? _firstLineIndent;
        private bool? _suppressHyphenation;
        private bool? _suppressLineNumbers;
        private int? _outlineLevel;
        private bool? _widowOrphanControl;
        private bool? _keepWithNext;
        private bool? _keepLinesTogether;
        private bool? _pageBreakBefore;
        private string _fontName;
        private string _fontNameAscii;
        private string _fontNameHighAnsi;
        private string _fontNameComplexScript;
        private string _fontNameEastAsia;
        
        private float? _fontSize;
        private bool? _bold;
        private bool? _italic;
        private bool _used;
        private bool _isDeleted;
        private bool _isDefault;
        private bool? _allCaps;
        private Color? _backColor;
        private bool? _contextualSpacing;
        private Color? _foreColor;
        private bool? _hidden;
        private LangInfo? _language;
        private bool? _noProof;
        private Color? _highlightColor;
        private float? _kerningThreshold;
        private int? _scale;
        private bool? _smallCaps;
        private float? _position;
        private bool? _snapToGrid;
        private int? _spacing;
        private StrikeoutType? _strikeout;
        private bool? _subscript;
        private bool? _superscript;
        private Color? _underlineColor;

        public DocumentStyle(){
            this.InitializeMap();
        }

        [Browsable(false)]
        Dictionary<string, string> IDocumentStyle.PropeprtiesMap{ get; } = new Dictionary<string, string>();


        [XafDisplayName("Type")]
        public DocumentStyleType DocumentStyleType{
            get => _documentStyleType;
            set{
                if (value == _documentStyleType) return;
                _documentStyleType = value;
                OnPropertyChanged();
            }
        }

        [XafDisplayName("Name")]
        public string StyleName{
            get => _styleName;
            set{
                if (value == _styleName) return;
                _styleName = value;
                OnPropertyChanged();
            }
        }

        [XafDisplayName("Underline")]
        public UnderlineType? Underline{
            get => _underline;
            set{
                if (value == _underline) return;
                _underline = value;
                OnPropertyChanged();
            }
        }

        void CharacterPropertiesBase.Reset(){
            throw new NotImplementedException();
        }

        void ParagraphPropertiesBase.Reset(ParagraphPropertiesMask mask){
            throw new NotImplementedException();
        }

        void ParagraphPropertiesBase.Assign(ParagraphPropertiesBase source){
            throw new NotImplementedException();
        }

        void ParagraphPropertiesBase.Reset(){
            throw new NotImplementedException();
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public ParagraphAlignment? Alignment{
            get => _alignment;
            set{
                if (value == _alignment) return;
                _alignment = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public bool? RightToLeft{
            get => _rightToLeft;
            set{
                if (value == _rightToLeft) return;
                _rightToLeft = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public float? LeftIndent{
            get => _leftIndent;
            set{
                if (Nullable.Equals(value, _leftIndent)) return;
                _leftIndent = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public float? RightIndent{
            get => _rightIndent;
            set{
                if (Nullable.Equals(value, _rightIndent)) return;
                _rightIndent = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public float? SpacingBefore{
            get => _spacingBefore;
            set{
                if (Nullable.Equals(value, _spacingBefore)) return;
                _spacingBefore = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public float? SpacingAfter{
            get => _spacingAfter;
            set{
                if (Nullable.Equals(value, _spacingAfter)) return;
                _spacingAfter = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public ParagraphLineSpacing? LineSpacingType{
            get => _lineSpacingType;
            set{
                if (value == _lineSpacingType) return;
                _lineSpacingType = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public float? LineSpacing{
            get => _lineSpacing;
            set{
                if (Nullable.Equals(value, _lineSpacing)) return;
                _lineSpacing = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public float? LineSpacingMultiplier{
            get => _lineSpacingMultiplier;
            set{
                if (Nullable.Equals(value, _lineSpacingMultiplier)) return;
                _lineSpacingMultiplier = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public ParagraphFirstLineIndent? FirstLineIndentType{
            get => _firstLineIndentType;
            set{
                if (value == _firstLineIndentType) return;
                _firstLineIndentType = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public float? FirstLineIndent{
            get => _firstLineIndent;
            set{
                if (Nullable.Equals(value, _firstLineIndent)) return;
                _firstLineIndent = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public bool? SuppressHyphenation{
            get => _suppressHyphenation;
            set{
                if (value == _suppressHyphenation) return;
                _suppressHyphenation = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public bool? SuppressLineNumbers{
            get => _suppressLineNumbers;
            set{
                if (value == _suppressLineNumbers) return;
                _suppressLineNumbers = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public int? OutlineLevel{
            get => _outlineLevel;
            set{
                if (value == _outlineLevel) return;
                _outlineLevel = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public bool? WidowOrphanControl{
            get => _widowOrphanControl;
            set{
                if (value == _widowOrphanControl) return;
                _widowOrphanControl = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public bool? KeepWithNext{
            get => _keepWithNext;
            set{
                if (value == _keepWithNext) return;
                _keepWithNext = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public bool? KeepLinesTogether{
            get => _keepLinesTogether;
            set{
                if (value == _keepLinesTogether) return;
                _keepLinesTogether = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public bool? PageBreakBefore{
            get => _pageBreakBefore;
            set{
                if (value == _pageBreakBefore) return;
                _pageBreakBefore = value;
                OnPropertyChanged();
            }
        }

        void CharacterPropertiesBase.Reset(CharacterPropertiesMask mask){
            throw new NotImplementedException();
        }

        void CharacterPropertiesBase.Assign(CharacterPropertiesBase source){
            throw new NotImplementedException();
        }

        [XafDisplayName("Font")]
        public string FontName{
            get => _fontName;
            set{
                if (value == _fontName) return;
                _fontName = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public string FontNameAscii{
            get => _fontNameAscii;
            set{
                if (value == _fontNameAscii) return;
                _fontNameAscii = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public string FontNameHighAnsi{
            get => _fontNameHighAnsi;
            set{
                if (value == _fontNameHighAnsi) return;
                _fontNameHighAnsi = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public string FontNameComplexScript{
            get => _fontNameComplexScript;
            set{
                if (value == _fontNameComplexScript) return;
                _fontNameComplexScript = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public string FontNameEastAsia{
            get => _fontNameEastAsia;
            set{
                if (value == _fontNameEastAsia) return;
                _fontNameEastAsia = value;
                OnPropertyChanged();
            }
        }

#if !XAF191
        private ThemeFont? _themeFontAscii;
        private ThemeFont? _themeFontHighAnsi;
        private ThemeFont? _themeFontComplexScript;
        private ThemeFont? _themeFontEastAsia;
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public ThemeFont? ThemeFontAscii{
            get => _themeFontAscii;
            set{
                if (value == _themeFontAscii) return;
                _themeFontAscii = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public ThemeFont? ThemeFontHighAnsi{
            get => _themeFontHighAnsi;
            set{
                if (value == _themeFontHighAnsi) return;
                _themeFontHighAnsi = value;
                OnPropertyChanged();
            }
        }

        public ThemeFont? ThemeFontComplexScript{
            get => _themeFontComplexScript;
            set{
                if (value == _themeFontComplexScript) return;
                _themeFontComplexScript = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public ThemeFont? ThemeFontEastAsia{
            get => _themeFontEastAsia;
            set{
                if (value == _themeFontEastAsia) return;
                _themeFontEastAsia = value;
                OnPropertyChanged();
            }
        }
#endif

        [XafDisplayName("Size")]
        public float? FontSize{
            get => _fontSize;
            set{
                if (Nullable.Equals(value, _fontSize)) return;
                _fontSize = value;
                OnPropertyChanged();
            }
        }

        [XafDisplayName("Bold")]
        public bool? Bold{
            get => _bold;
            set{
                if (value == _bold) return;
                _bold = value;
                OnPropertyChanged();
            }
        }

        [XafDisplayName("Italic")]
        public bool? Italic{
            get => _italic;
            set{
                if (value == _italic) return;
                _italic = value;
                OnPropertyChanged();
            }
        }


        public override string ToString(){
            return $"{DocumentStyleType}: {StyleName}";
        }

        protected bool Equals(DocumentStyle other){
            return StyleName == other.StyleName;
        }

        public override bool Equals(object obj){
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DocumentStyle) obj);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode(){
            return (StyleName != null ? StyleName.GetHashCode() : 0);
        }

        [VisibleInListView(false)]
        public bool Used{
            get => _used;
            set{
                if (value == _used) return;
                _used = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        public bool IsDeleted{
            get => _isDeleted;
            set{
                if (value == _isDeleted) return;
                _isDeleted = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        public bool IsDefault{
            get => _isDefault;
            set{
                if (value == _isDefault) return;
                _isDefault = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public bool? AllCaps{
            get => _allCaps;
            set{
                if (value == _allCaps) return;
                _allCaps = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public Color? BackColor{
            get => _backColor;
            set{
                if (Nullable.Equals(value, _backColor)) return;
                _backColor = value;
                OnPropertyChanged();
            }
        }

        public bool? ContextualSpacing{
            get => _contextualSpacing;
            set{
                if (value == _contextualSpacing) return;
                _contextualSpacing = value;
                OnPropertyChanged();
            }
        }

        private ParagraphBorders Borders { get; set; }

        ParagraphBorders ParagraphPropertiesBase.Borders => Borders;

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public Color? ForeColor{
            get => _foreColor;
            set{
                if (Nullable.Equals(value, _foreColor)) return;
                _foreColor = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public bool? Hidden{
            get => _hidden;
            set{
                if (value == _hidden) return;
                _hidden = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public LangInfo? Language{
            get => _language;
            set{
                if (Nullable.Equals(value, _language)) return;
                _language = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public bool? NoProof{
            get => _noProof;
            set{
                if (value == _noProof) return;
                _noProof = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public Color? HighlightColor{
            get => _highlightColor;
            set{
                if (Nullable.Equals(value, _highlightColor)) return;
                _highlightColor = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public float? KerningThreshold{
            get => _kerningThreshold;
            set{
                if (Nullable.Equals(value, _kerningThreshold)) return;
                _kerningThreshold = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public int? Scale{
            get => _scale;
            set{
                if (value == _scale) return;
                _scale = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public bool? SmallCaps{
            get => _smallCaps;
            set{
                if (value == _smallCaps) return;
                _smallCaps = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public float? Position{
            get => _position;
            set{
                if (Nullable.Equals(value, _position)) return;
                _position = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public bool? SnapToGrid{
            get => _snapToGrid;
            set{
                if (value == _snapToGrid) return;
                _snapToGrid = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public int? Spacing{
            get => _spacing;
            set{
                if (value == _spacing) return;
                _spacing = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public StrikeoutType? Strikeout{
            get => _strikeout;
            set{
                if (value == _strikeout) return;
                _strikeout = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public bool? Subscript{
            get => _subscript;
            set{
                if (value == _subscript) return;
                _subscript = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public bool? Superscript{
            get => _superscript;
            set{
                if (value == _superscript) return;
                _superscript = value;
                OnPropertyChanged();
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public Color? UnderlineColor{
            get => _underlineColor;
            set{
                if (Nullable.Equals(value, _underlineColor)) return;
                _underlineColor = value;
                OnPropertyChanged();
            }
        }

        [Browsable(false)]
        IDocumentStyle IDocumentStyle.Parent{ get; set; }
        
        
        public DocumentStyle Parent{
	        get => (DocumentStyle) ((IDocumentStyle) this).Parent;
	        set => ((IDocumentStyle) this).Parent=value;
        }
        
        
        public DocumentStyle Next{
	        get => (DocumentStyle) ((IDocumentStyle) this).Next;
	        set => ((IDocumentStyle) this).Next=value;
        }

        [Browsable(false)]
        IDocumentStyle IDocumentStyle.Next{ get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null){
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
    }

}