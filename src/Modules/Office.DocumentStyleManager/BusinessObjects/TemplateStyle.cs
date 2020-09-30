using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using DevExpress.Xpo;
using DevExpress.XtraRichEdit.API.Native;
using DevExpress.XtraRichEdit.Model;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Persistent.BaseImpl;
using ParagraphAlignment = DevExpress.XtraRichEdit.API.Native.ParagraphAlignment;
using ParagraphFirstLineIndent = DevExpress.XtraRichEdit.API.Native.ParagraphFirstLineIndent;
using ParagraphLineSpacing = DevExpress.XtraRichEdit.API.Native.ParagraphLineSpacing;
using StrikeoutType = DevExpress.XtraRichEdit.API.Native.StrikeoutType;
using UnderlineType = DevExpress.XtraRichEdit.API.Native.UnderlineType;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects{
	[DefaultProperty(nameof(StyleName))]
	public class TemplateStyle:CustomBaseObject,IDocumentStyle{
		public TemplateStyle(Session session) : base(session){
		}

		DocumentStyleType _documentStyleType;

		public DocumentStyleType DocumentStyleType{
			get => _documentStyleType;
			set => SetPropertyValue(nameof(DocumentStyleType), ref _documentStyleType, value);
		}

		string _styleName;

		public string StyleName{
			get => _styleName;
			set => SetPropertyValue(nameof(StyleName), ref _styleName, value);
		}

		UnderlineType? _underlineType;

		public UnderlineType? Underline{
			get => _underlineType;
			set => SetPropertyValue(nameof(Underline), ref _underlineType, value);
		}

		string _fontName;

		void CharacterPropertiesBase.Reset(){
			throw new System.NotImplementedException();
		}

		void ParagraphPropertiesBase.Reset(ParagraphPropertiesMask mask){
			throw new System.NotImplementedException();
		}

		void ParagraphPropertiesBase.Assign(ParagraphPropertiesBase source){
			throw new System.NotImplementedException();
		}

		ParagraphAlignment? _alignment;

		void ParagraphPropertiesBase.Reset(){
			throw new System.NotImplementedException();
		}

		public ParagraphAlignment? Alignment{
			get => _alignment;
			set => SetPropertyValue(nameof(Alignment), ref _alignment, value);
		}

		bool? _rightToLeft;

		public bool? RightToLeft{
			get => _rightToLeft;
			set => SetPropertyValue(nameof(RightToLeft), ref _rightToLeft, value);
		}

		float? _leftIndent;

		public float? LeftIndent{
			get => _leftIndent;
			set => SetPropertyValue(nameof(LeftIndent), ref _leftIndent, value);
		}

		float? _rightIndent;

		public float? RightIndent{
			get => _rightIndent;
			set => SetPropertyValue(nameof(RightIndent), ref _rightIndent, value);
		}

		float? _spacingBefore;

		public float? SpacingBefore{
			get => _spacingBefore;
			set => SetPropertyValue(nameof(SpacingBefore), ref _spacingBefore, value);
		}

		float? _spacingAfter;

		public float? SpacingAfter{
			get => _spacingAfter;
			set => SetPropertyValue(nameof(SpacingAfter), ref _spacingAfter, value);
		}

		ParagraphLineSpacing? _lineSpacingType;

		public ParagraphLineSpacing? LineSpacingType{
			get => _lineSpacingType;
			set => SetPropertyValue(nameof(LineSpacingType), ref _lineSpacingType, value);
		}

		float? _lineSpacing;

		public float? LineSpacing{
			get => _lineSpacing;
			set => SetPropertyValue(nameof(LineSpacing), ref _lineSpacing, value);
		}

		float? _lineSpacingMultiplier;

		public float? LineSpacingMultiplier{
			get => _lineSpacingMultiplier;
			set => SetPropertyValue(nameof(LineSpacingMultiplier), ref _lineSpacingMultiplier, value);
		}

		ParagraphFirstLineIndent? _firstLineIndentType;

		public ParagraphFirstLineIndent? FirstLineIndentType{
			get => _firstLineIndentType;
			set => SetPropertyValue(nameof(FirstLineIndentType), ref _firstLineIndentType, value);
		}

		float? _firstLineIndent;

		public float? FirstLineIndent{
			get => _firstLineIndent;
			set => SetPropertyValue(nameof(FirstLineIndent), ref _firstLineIndent, value);
		}

		bool? _suppressHyphenation;

		public bool? SuppressHyphenation{
			get => _suppressHyphenation;
			set => SetPropertyValue(nameof(SuppressHyphenation), ref _suppressHyphenation, value);
		}

		bool? _suppressLineNumbers;

		public bool? SuppressLineNumbers{
			get => _suppressLineNumbers;
			set => SetPropertyValue(nameof(SuppressLineNumbers), ref _suppressLineNumbers, value);
		}

		int? _outlineLevel;

		public int? OutlineLevel{
			get => _outlineLevel;
			set => SetPropertyValue(nameof(OutlineLevel), ref _outlineLevel, value);
		}

		bool? _widowOrphanControl;

		public bool? WidowOrphanControl{
			get => _widowOrphanControl;
			set => SetPropertyValue(nameof(WidowOrphanControl), ref _widowOrphanControl, value);
		}

		bool? _keepWithNext;

		public bool? KeepWithNext{
			get => _keepWithNext;
			set => SetPropertyValue(nameof(KeepWithNext), ref _keepWithNext, value);
		}

		bool? _keepLinesTogether;

		public bool? KeepLinesTogether{
			get => _keepLinesTogether;
			set => SetPropertyValue(nameof(KeepLinesTogether), ref _keepLinesTogether, value);
		}

		bool? _pageBreakBefore;

		public bool? PageBreakBefore{
			get => _pageBreakBefore;
			set => SetPropertyValue(nameof(PageBreakBefore), ref _pageBreakBefore, value);
		}

		void CharacterPropertiesBase.Reset(CharacterPropertiesMask mask){
			throw new System.NotImplementedException();
		}

		void CharacterPropertiesBase.Assign(CharacterPropertiesBase source){
			throw new System.NotImplementedException();
		}

		public string FontName{
			get => _fontName;
			set => SetPropertyValue(nameof(FontName), ref _fontName, value);
		}

		string _fontNameAscii;

		public string FontNameAscii{
			get => _fontNameAscii;
			set => SetPropertyValue(nameof(FontNameAscii), ref _fontNameAscii, value);
		}
		
		string _fontNameHighAnsi;

		public string FontNameHighAnsi{
			get => _fontNameHighAnsi;
			set => SetPropertyValue(nameof(FontNameHighAnsi), ref _fontNameHighAnsi, value);
		}
		
		string _fontNameComplexScript;

		public string FontNameComplexScript{
			get => _fontNameComplexScript;
			set => SetPropertyValue(nameof(FontNameComplexScript), ref _fontNameComplexScript, value);
		}
		string _fontNameEastAsia;

		public string FontNameEastAsia{
			get => _fontNameEastAsia;
			set => SetPropertyValue(nameof(FontNameEastAsia), ref _fontNameEastAsia, value);
		}
#if !XAF191
        ThemeFont? _themeFontAscii;

        public ThemeFont? ThemeFontAscii{
            get => _themeFontAscii;
            set => SetPropertyValue(nameof(ThemeFontAscii), ref _themeFontAscii, value);
        }
        ThemeFont? _themeFontHighAnsi;

        public ThemeFont? ThemeFontHighAnsi{
            get => _themeFontHighAnsi;
            set => SetPropertyValue(nameof(ThemeFontHighAnsi), ref _themeFontHighAnsi, value);
        }
        ThemeFont? _themeFontComplexScript;

        public ThemeFont? ThemeFontComplexScript{
            get => _themeFontComplexScript;
            set => SetPropertyValue(nameof(ThemeFontComplexScript), ref _themeFontComplexScript, value);
        }
        ThemeFont? _themeFontEastAsia;

        public ThemeFont? ThemeFontEastAsia{
            get => _themeFontEastAsia;
            set => SetPropertyValue(nameof(ThemeFontEastAsia), ref _themeFontEastAsia, value);
        }
#endif

		float? _fontSize;

		public float? FontSize{
			get => _fontSize;
			set => SetPropertyValue(nameof(FontSize), ref _fontSize, value);
		}


		bool? _fontBold;

		public bool? Bold{
			get => _fontBold;
			set => SetPropertyValue(nameof(Bold), ref _fontBold, value);
		}


		bool? _fontItalic;

		public bool? Italic{
			get => _fontItalic;
			set => SetPropertyValue(nameof(Italic), ref _fontItalic, value);
		}

		bool IDocumentStyle.Used{ get; set; }
		bool IDocumentStyle.IsDeleted{ get; set; }
		bool _isDefault;

		public bool IsDefault{
			get => _isDefault;
			set => SetPropertyValue(nameof(IsDefault), ref _isDefault, value);
		}

		bool? _allCaps;

		public bool? AllCaps{
			get => _allCaps;
			set => SetPropertyValue(nameof(AllCaps), ref _allCaps, value);
		}

		Color? _backColor;
		[ValueConverter(typeof(ColorValueConverter))]
		public Color? BackColor{
			get => _backColor;
			set => SetPropertyValue(nameof(BackColor), ref _backColor, value);
		}

		public bool? ContextualSpacing{ get; set; }

		Color? _foreColor;
        [ValueConverter(typeof(ColorValueConverter))]
		public Color? ForeColor{
			get => _foreColor;
			set => SetPropertyValue(nameof(ForeColor), ref _foreColor, value);
		}

		bool? _hidden;

		public bool? Hidden{
			get => _hidden;
			set => SetPropertyValue(nameof(Hidden), ref _hidden, value);
		}

		public LangInfo? Language{ get; set; }
		public bool? NoProof{ get; set; }

		Color? _highlightColor;
        [ValueConverter(typeof(ColorValueConverter))]
		public Color? HighlightColor{
			get => _highlightColor;
			set => SetPropertyValue(nameof(HighlightColor), ref _highlightColor, value);
		}

		float? _kerningThreshold;

		public float? KerningThreshold{
			get => _kerningThreshold;
			set => SetPropertyValue(nameof(KerningThreshold), ref _kerningThreshold, value);
		}

		int? _scale;

		public int? Scale{
			get => _scale;
			set => SetPropertyValue(nameof(Scale), ref _scale, value);
		}

		bool? _smallCaps;

		public bool? SmallCaps{
			get => _smallCaps;
			set => SetPropertyValue(nameof(SmallCaps), ref _smallCaps, value);
		}

		float? _position;

		public float? Position{
			get => _position;
			set => SetPropertyValue(nameof(Position), ref _position, value);
		}

		bool? _snapToGrid;

		public bool? SnapToGrid{
			get => _snapToGrid;
			set => SetPropertyValue(nameof(SnapToGrid), ref _snapToGrid, value);
		}

		int? _spacing;

		public int? Spacing{
			get => _spacing;
			set => SetPropertyValue(nameof(Spacing), ref _spacing, value);
		}

		StrikeoutType? _strikeout;

		public StrikeoutType? Strikeout{
			get => _strikeout;
			set => SetPropertyValue(nameof(Strikeout), ref _strikeout, value);
		}

		bool? _subscript;

		public bool? Subscript{
			get => _subscript;
			set => SetPropertyValue(nameof(Subscript), ref _subscript, value);
		}

		bool? _superscript;

		public bool? Superscript{
			get => _superscript;
			set => SetPropertyValue(nameof(Superscript), ref _superscript, value);
		}

		Color? _underlineColor;
        [ValueConverter(typeof(ColorValueConverter))]
		public Color? UnderlineColor{
			get => _underlineColor;
			set => SetPropertyValue(nameof(UnderlineColor), ref _underlineColor, value);
		}

		TemplateStyle _parent;

		[PublicAPI]
		public TemplateStyle Parent{
			get => _parent;
			set => SetPropertyValue(nameof(Parent), ref _parent, value);
		}
		IDocumentStyle IDocumentStyle.Parent{ get; set; }

		TemplateStyle _next;

		[PublicAPI]
		public TemplateStyle Next{
			get => _next;
			set => SetPropertyValue(nameof(Next), ref _next, value);
		}

		Dictionary<string, string> IDocumentStyle.PropeprtiesMap{ get; } = new Dictionary<string, string>();

		IDocumentStyle IDocumentStyle.Next{ get; set; }


		public override string ToString(){
			return $"{DocumentStyleType}: {StyleName}";
		}


	}
}