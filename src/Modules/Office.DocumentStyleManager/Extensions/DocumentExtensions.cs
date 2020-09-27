using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Layout;
using DevExpress.XtraRichEdit.API.Native;
using Fasterflect;
using JetBrains.Annotations;
using Xpand.Extensions.ExpressionExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions{
    public static class DocumentExtensions{
	    private const string DefaultDocumentStyleName = "DefaultDocumentStyleName";

	    [PublicAPI]
        public static IEnumerable<IDocumentStyle> WhenDefaultStyle(this IEnumerable<IDocumentStyle> source) => source.Where(_ => _.IsDefault);
        public static IEnumerable<IDocumentStyle> WhenNotDefaultStyle(this IEnumerable<IDocumentStyle> source) => source.Where(_ => !_.IsDefault);

        [PublicAPI]
        public static void ApplyStyle(this Document document,IDocumentStyle documentStyle,Document defaultPropertiesProvider=null){
	        defaultPropertiesProvider ??= document;
            if (documentStyle.DocumentStyleType==DocumentStyleType.Paragraph){
                document.BeginUpdate();
                var paragraphFromPosition = document.ParagraphFromPosition();
                document.Paragraphs[paragraphFromPosition].Style = (ParagraphStyle) documentStyle.Get(document,defaultPropertiesProvider);
                document.EndUpdate();
            }
            else{
                var range = document.WordFromPosition();
                var charProps = document.BeginUpdateCharacters(range);
                charProps.Style = (CharacterStyle) documentStyle.Get(document,defaultPropertiesProvider);
                document.EndUpdateCharacters(charProps);
            }
        }

        public static bool Ensure(this IDocumentStyle documentStyle, Document document,IDocumentStyle[] usedStyles=null,Document defaultPropertiesProvider=null){
			defaultPropertiesProvider??=document;
	        usedStyles ??= document.UsedStyles(documentStyle.DocumentStyleType, defaultPropertiesProvider).ToArray();
	        if (usedStyles.Contains(documentStyle)){
		        return false;
	        }

	        documentStyle.Get(document, defaultPropertiesProvider);
	        return true;
        }

        public static CharacterPropertiesBase Get(this IDocumentStyle documentStyle, Document document,Document defaultPropertiesProvider=null) => documentStyle
	        .Find( document) ?? document.CreateNewStyle(documentStyle,defaultPropertiesProvider);

        public static CharacterPropertiesBase Find(this IDocumentStyle documentStyle, Document document) =>
	        documentStyle.DocumentStyleType==DocumentStyleType.Paragraph ?(CharacterPropertiesBase) document.ParagraphStyles
		        .FirstOrDefault(_ => _.Name==documentStyle.StyleName):document.CharacterStyles
		        .FirstOrDefault(_ => _.Name==documentStyle.StyleName);

        public static CharacterPropertiesBase CreateNewStyle(this Document document,IDocumentStyle documentStyle,Document defaultPropertiesProvider=null){
	        if (defaultPropertiesProvider != null){
		        defaultPropertiesProvider.DefaultCharacterProperties.MapProperties(document.DefaultCharacterProperties);
		        defaultPropertiesProvider.DefaultParagraphProperties.MapProperties(document.DefaultParagraphProperties);
	        }
	        var style = documentStyle.DocumentStyleType == DocumentStyleType.Paragraph
		        ? (CharacterPropertiesBase) document.ParagraphStyles.CreateNew()
		        : document.CharacterStyles.CreateNew();
	        document.MapCommonProperties(style,documentStyle);
	        if (style is ParagraphStyle paragraphStyle){
		        document.MapParagraphProperties(paragraphStyle, documentStyle);
		        var styleNext = documentStyle.Next;
		        if (styleNext != null){
			        paragraphStyle.NextStyle = styleNext.StyleName == paragraphStyle.Name()
				        ? paragraphStyle : (ParagraphStyle) styleNext.Get(document,defaultPropertiesProvider);
		        }
                var existingStyle = document.ParagraphStyles.FirstOrDefault(_ => _.Name == style.Name());
                if (existingStyle!=null){
                    return existingStyle;
                }
                document.ParagraphStyles.Add(paragraphStyle);
            }
            else{
                var existingStyle = document.CharacterStyles.FirstOrDefault(_ => _.Name == style.Name());
                if (existingStyle!=null){
                    return existingStyle;
                }
                document.CharacterStyles.Add((CharacterStyle) style);
            }
            return style;
        }

        private static void MapParagraphProperties(this Document document, ParagraphStyle style, IDocumentStyle documentStyle){
	        style.Alignment = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.Alignment);
	        style.RightToLeft = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.RightToLeft);
	        style.LeftIndent = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.LeftIndent);
	        style.RightIndent = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.LeftIndent);
	        style.SpacingBefore = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.LeftIndent);
	        style.SpacingAfter = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.LeftIndent);
	        style.LineSpacingType = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.LineSpacingType);
	        style.LineSpacing = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.LineSpacing);
	        style.LineSpacingMultiplier = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.LineSpacingMultiplier);
	        style.FirstLineIndentType = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.FirstLineIndentType);
	        style.FirstLineIndent = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.FirstLineIndent);
	        style.SuppressHyphenation = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.SuppressHyphenation);
	        style.SuppressLineNumbers = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.SuppressLineNumbers);
	        style.OutlineLevel = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.OutlineLevel);
	        style.WidowOrphanControl = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.WidowOrphanControl);
	        style.KeepWithNext = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.KeepWithNext);
	        style.KeepLinesTogether = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.KeepLinesTogether);
	        style.PageBreakBefore = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.PageBreakBefore);
	        style.ContextualSpacing = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.ContextualSpacing);
        }

        private static void MapCommonProperties(this Document document,CharacterPropertiesBase style, IDocumentStyle documentStyle){
	        style.SetPropertyValue("Name",documentStyle.StyleName);
	        style.AllCaps = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.AllCaps);
	        style.BackColor = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.BackColor);
	        style.ForeColor = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.ForeColor);
	        style.Hidden = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.Hidden);
	        style.HighlightColor = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.HighlightColor);
#if !XAF191 && !XAF192
            style.KerningThreshold = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.KerningThreshold);
            style.Position = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.Position);
            style.Scale = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.Scale);
            style.SnapToGrid = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.SnapToGrid);
            style.Spacing = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.Spacing);
#endif
#if !XAF191
            style.SmallCaps = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.SmallCaps);
#endif
	        style.Strikeout = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.Strikeout);
	        style.Subscript = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.Subscript);
	        style.Superscript = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.Superscript);
	        style.UnderlineColor = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.UnderlineColor);
	        style.Italic = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.Italic);
	        style.Bold = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.Bold);
	        style.Underline = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.Underline);
	        style.FontName = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.FontName);
	        style.FontSize = documentStyle.GetStylePropertyValue(documentStyle, document, _ => _.FontSize);
	        style.SetPropertyValue("Parent", documentStyle.Parent?.Get(document));
        }

        internal static void InitializeMap(this IDocumentStyle style){
	        var propertyInfos = style.GetType().Properties();
	        foreach (var propertyInfo in propertyInfos){
		        style.PropeprtiesMap.Add(propertyInfo.Name, DefaultDocumentStyleName);
	        }
        }

        private static TProperty GetStylePropertyValue<TStyle, TProperty>(this TStyle style, IDocumentStyle documentStyle, Document document,
	        Expression<Func<IDocumentStyle, TProperty>> valueSelector) where TStyle:IDocumentStyle =>
	        style.GetStylePropertyValueCore<TProperty>(valueSelector.MemberExpressionName(),documentStyle,document);

        private static TValue GetStylePropertyValueCore<TValue>(this IDocumentStyle style, string propertyName,
	        IDocumentStyle documentStyle, Document document){
	        var styleName = documentStyle.PropeprtiesMap[propertyName];
	        
	        if (styleName == style.StyleName){
		        return (TValue) style.GetPropertyValue(propertyName);
	        }
	        if (styleName != DefaultDocumentStyleName){
		        var parentStyle = style.FromHierarchy(_ => _.Parent).First(_ => _.StyleName==styleName);
		        parentStyle.Get(document);
	        }
	        
	        return default;
        }

        public static DocumentStyle ToDocumentStyle(this CharacterPropertiesBase style,Document defaultPropertiesProvider,  bool used = false,bool isDefault=false){
	        var isParagraphStyle = style.IsParagraphStyle();
	        var documentStyle = new DocumentStyle{
		        DocumentStyleType = isParagraphStyle ? DocumentStyleType.Paragraph : DocumentStyleType.Character,
		        Used = used, IsDefault = isDefault
	        };

	        documentStyle.MapProperties(style, defaultPropertiesProvider);
	        documentStyle.Parent = style.Parent()?.ToDocumentStyle(defaultPropertiesProvider,used, isDefault);
	        documentStyle.Next = (DocumentStyle) style.NextDocumentStyle( used, isDefault,documentStyle,defaultPropertiesProvider);
	        return documentStyle;
        }

        public static bool IsParagraphStyle(this CharacterPropertiesBase style) => 
	        style is ParagraphStyle || (style is IDocumentStyle docStyle && docStyle.DocumentStyleType == DocumentStyleType.Paragraph);

        private static void MapProperties(this IDocumentStyle documentStyle,CharacterPropertiesBase style, Document defaultPropertiesProvider){
	        documentStyle.IsDeleted = (bool) style.GetPropertyValue("IsDeleted");
	        documentStyle.StyleName = style.Name();
	        if (style is ParagraphPropertiesBase paragraphProperties){
		        var defaultParagraphProperties = defaultPropertiesProvider.DefaultParagraphProperties;
		        documentStyle.Alignment = paragraphProperties.GetStylePropertyValue(c => c.Alignment, documentStyle, defaultParagraphProperties);
		        documentStyle.RightToLeft = paragraphProperties.GetStylePropertyValue(c => c.RightToLeft, documentStyle, defaultParagraphProperties);
		        documentStyle.LeftIndent = paragraphProperties.GetStylePropertyValue(c => c.LeftIndent, documentStyle, defaultParagraphProperties);
		        documentStyle.RightIndent = paragraphProperties.GetStylePropertyValue(c => c.RightIndent, documentStyle, defaultParagraphProperties);
		        documentStyle.SpacingBefore = paragraphProperties.GetStylePropertyValue(c => c.SpacingBefore, documentStyle, defaultParagraphProperties);
		        documentStyle.SpacingAfter = paragraphProperties.GetStylePropertyValue(c => c.SpacingAfter, documentStyle, defaultParagraphProperties);
		        documentStyle.LineSpacingType = paragraphProperties.GetStylePropertyValue(c => c.LineSpacingType, documentStyle, defaultParagraphProperties);
		        documentStyle.FirstLineIndent = paragraphProperties.GetStylePropertyValue(c => c.FirstLineIndent, documentStyle, defaultParagraphProperties);
		        documentStyle.SuppressHyphenation = paragraphProperties.GetStylePropertyValue(c => c.SuppressHyphenation, documentStyle, defaultParagraphProperties);
		        documentStyle.SuppressLineNumbers = paragraphProperties.GetStylePropertyValue(c => c.SuppressLineNumbers, documentStyle, defaultParagraphProperties);
		        documentStyle.OutlineLevel = paragraphProperties.GetStylePropertyValue(c => c.OutlineLevel, documentStyle, defaultParagraphProperties);
		        documentStyle.WidowOrphanControl = paragraphProperties.GetStylePropertyValue(c => c.WidowOrphanControl, documentStyle, defaultParagraphProperties);
		        documentStyle.KeepWithNext = paragraphProperties.GetStylePropertyValue(c => c.KeepWithNext, documentStyle, defaultParagraphProperties);
		        documentStyle.KeepLinesTogether = paragraphProperties.GetStylePropertyValue(c => c.KeepLinesTogether, documentStyle, defaultParagraphProperties);
		        documentStyle.PageBreakBefore = paragraphProperties.GetStylePropertyValue(c => c.PageBreakBefore, documentStyle, defaultParagraphProperties);
		        documentStyle.BackColor = paragraphProperties.GetStylePropertyValue(c => c.BackColor, documentStyle, defaultParagraphProperties);
		        documentStyle.ContextualSpacing = paragraphProperties.GetStylePropertyValue(c => c.ContextualSpacing, documentStyle, defaultParagraphProperties);
		        
	        }

	        var defaultCharacterProperties = defaultPropertiesProvider.DefaultCharacterProperties;
	        documentStyle.FontName = style.GetStylePropertyValue(c => c.FontName, documentStyle, defaultCharacterProperties);
	        documentStyle.Bold = style.GetStylePropertyValue(c => c.Bold, documentStyle, defaultCharacterProperties);
	        documentStyle.Italic = style.GetStylePropertyValue(c => c.Italic, documentStyle, defaultCharacterProperties);
	        documentStyle.FontSize = style.GetStylePropertyValue(c => c.FontSize, documentStyle, defaultCharacterProperties);
	        documentStyle.Underline = style.GetStylePropertyValue(c => c.Underline, documentStyle, defaultCharacterProperties);
	        documentStyle.AllCaps = style.GetStylePropertyValue(c => c.AllCaps, documentStyle, defaultCharacterProperties);
	        documentStyle.BackColor = style.GetStylePropertyValue(c => c.BackColor, documentStyle, defaultCharacterProperties);
	        documentStyle.ForeColor = style.GetStylePropertyValue(c => c.ForeColor, documentStyle, defaultCharacterProperties);
	        documentStyle.Hidden = style.GetStylePropertyValue(c => c.Hidden, documentStyle, defaultCharacterProperties);
	        documentStyle.HighlightColor = style.GetStylePropertyValue(c => c.HighlightColor, documentStyle, defaultCharacterProperties);
#if !XAF191 && !XAF192
            documentStyle.KerningThreshold = style.GetStylePropertyValue(c => c.KerningThreshold, documentStyle, defaultCharacterProperties);
            documentStyle.Scale = style.GetStylePropertyValue(c => c.Scale, documentStyle, defaultCharacterProperties);
            documentStyle.Position = style.GetStylePropertyValue(c => c.Position, documentStyle, defaultCharacterProperties);
            documentStyle.SnapToGrid = style.GetStylePropertyValue(c => c.SnapToGrid, documentStyle, defaultCharacterProperties);
            documentStyle.Spacing = style.GetStylePropertyValue(c => c.Spacing, documentStyle, defaultCharacterProperties);
#endif
#if !XAF191
            documentStyle.SmallCaps = style.GetStylePropertyValue(c => c.SmallCaps, documentStyle, defaultCharacterProperties);
#endif
	        documentStyle.Strikeout = style.GetStylePropertyValue(c => c.Strikeout, documentStyle, defaultCharacterProperties);
	        documentStyle.Subscript = style.GetStylePropertyValue(c => c.Subscript, documentStyle, defaultCharacterProperties);
	        documentStyle.Superscript = style.GetStylePropertyValue(c => c.Superscript, documentStyle, defaultCharacterProperties);
	        documentStyle.UnderlineColor = style.GetStylePropertyValue(c => c.UnderlineColor, documentStyle, defaultCharacterProperties);
        }

        private static IDocumentStyle NextDocumentStyle(this CharacterPropertiesBase style, bool used, bool isDefault, DocumentStyle documentStyle,Document defaultPropertiesProvider){
	        var next = style.Next();
	        return next?.Name()==style.Name() ? documentStyle : next?.ToDocumentStyle(defaultPropertiesProvider,used,isDefault);
        }

        private static CharacterPropertiesBase Next(this CharacterPropertiesBase style) =>
	        style is ParagraphStyle? ((CharacterPropertiesBase) style.GetPropertyValue("NextStyle")):null;

        private static CharacterPropertiesBase Parent(this CharacterPropertiesBase style) => ((CharacterPropertiesBase) style.GetPropertyValue("Parent"));

        private static TProperty GetStylePropertyValue<TStyle,TProperty>(this TStyle style, Expression<Func<TStyle, TProperty>> valueSelector,
	        IDocumentStyle documentStyle,TStyle defaultProperties) =>
	        (TProperty) style.GetStylePropertyValueCore(valueSelector.MemberExpressionName(),documentStyle,defaultProperties);

        private static object GetStylePropertyValueCore<TStyle>(this TStyle style,string propertyName,IDocumentStyle documentStyle,TStyle defaultProperties){
	        var propertyValue = style.GetPropertyValue(propertyName);
	        if (propertyValue.IsDefaultValue()){
		        object parent = ((CharacterPropertiesBase) style).Parent();
		        return parent != null ? parent.GetStylePropertyValueCore(propertyName, documentStyle, defaultProperties)
			        : defaultProperties.GetPropertyValue(propertyName);
	        }
	        documentStyle.PropeprtiesMap[propertyName] = ((CharacterPropertiesBase) style).Name();
	        return propertyValue;
        }

        public static IEnumerable<IDocumentStyle> AllStyles(this Document document, DocumentStyleType? type = null,Document defaultPropertiesProvider=null) => document
	        .UsedStyles(type,defaultPropertiesProvider).ToArray().Concat(document.UnusedStyles(type,defaultPropertiesProvider));

        public static IEnumerable<IDocumentStyle> UnusedStyles(this Document document,DocumentStyleType? type=null,Document defaultPropertiesProvider=null){
	        defaultPropertiesProvider ??= document;
            var usedStyles = document.UsedStyles(type,defaultPropertiesProvider).ToArray();
            return document.ParagraphStyles.Select(_ => _.ToDocumentStyle(isDefault: document.IsDefaultStyle(_, defaultPropertiesProvider),
		            defaultPropertiesProvider: defaultPropertiesProvider)).Where(_ => !usedStyles.Contains(_))
	            .Concat(document.CharacterStyles.Select(_ => _.ToDocumentStyle(isDefault: document.IsDefaultStyle(_, defaultPropertiesProvider),
			            defaultPropertiesProvider: defaultPropertiesProvider)).Where(_ => !usedStyles.Contains(_)))
	            .Where(style => type == null || style.DocumentStyleType == type.Value);
        }

        public static bool IsDefaultStyle(this Document document, CharacterPropertiesBase style,Document defaultPropertiesProvider=null){
	        defaultPropertiesProvider ??= document;
            var paragraphDefaultStyle = document.DefaultStyle(defaultPropertiesProvider,DocumentStyleType.Paragraph).StyleName;
            var characterDefaultStyle = document.DefaultStyle(defaultPropertiesProvider,DocumentStyleType.Character).StyleName;
            var name = style.Name();
            return style is ParagraphStyle ? name == paragraphDefaultStyle : name == characterDefaultStyle;
        }

        public static IDocumentStyle DefaultStyle(this Document document,Document defaultPropertiesProvider,DocumentStyleType type) =>
	        type == DocumentStyleType.Paragraph ? document.ParagraphStyles.First().ToDocumentStyle(defaultPropertiesProvider)
		        : document.CharacterStyles.First().ToDocumentStyle(defaultPropertiesProvider);

        public static IEnumerable<IDocumentStyle> UsedStyles(this Document document,DocumentStyleType? type=null,Document defaultPropertiesProvider=null){
	        defaultPropertiesProvider ??= document;
	        return document.UsedParagraphStyles(defaultPropertiesProvider)
		        .Concat(document.UsedCharacterStyles(defaultPropertiesProvider))
		        .Distinct().Where(style => type == null || style.DocumentStyleType == type.Value);
        }

        private static IEnumerable<DocumentStyle> UsedCharacterStyles(this Document document,Document defaultPropertiesProvider) =>
	        document.CharacterRanges().Select(_ => _.Key).Distinct()
		        .SelectMany(s => document.CharacterStyles.Where(style => style.Name() == s)
		        .SelectMany(style => style.FromHierarchy(characterStyle => characterStyle.Parent))
		        .DistinctBy(style => style.Name)
		        .Select(style => style.ToDocumentStyle(defaultPropertiesProvider,true,document.IsDefaultStyle(style,defaultPropertiesProvider))).Do(style => style.Used=true));

        private static IEnumerable<DocumentStyle> UsedParagraphStyles(this Document document,Document defaultPropertiesProvider) =>
	        document.Paragraphs.SelectMany(_ => _.Style.FromHierarchy(style => style.Parent)
		        .Concat(UsedNextStyles(_))
		        .WhereNotDefault()
		        .DistinctBy(style => style.Name)
		        .Select(style => style.ToDocumentStyle( defaultPropertiesProvider,true,document.IsDefaultStyle(style,defaultPropertiesProvider))).Do(style => style.Used=true));

        private static IEnumerable<ParagraphStyle> UsedNextStyles(Paragraph paragraph) =>
	        paragraph.Style.NextStyle?.NextStyle == paragraph.Style.NextStyle
		        ? new[]{paragraph.Style.NextStyle} : paragraph.Style.FromHierarchy(style => style.NextStyle,
			        style => style?.NextStyle?.Name != style?.Name);

        class CharacterStyleVisitor:DocumentVisitorBase{
            private readonly Document _document;
            public Dictionary<string,List<DocumentRange>> Ranges{ get; } = new Dictionary<string, List<DocumentRange>>();

            public CharacterStyleVisitor(Document document) => _document = document;

            public override void Visit(DocumentText text){
                base.Visit(text);
                if (!Ranges.ContainsKey(text.TextProperties.StyleName)){
                    Ranges.Add(text.TextProperties.StyleName,new List<DocumentRange>(){_document.CreateRange(text.Position,text.Length)});    
                }
                else{
                    Ranges[text.TextProperties.StyleName].Add(_document.CreateRange(text.Position,text.Length));
                }
            }
        }

        public static T Visit<T>(this Document document, T visitor) where T:DocumentVisitorBase{
            var iterator = new DocumentIterator(document,true);
            while (iterator.MoveNext())
                iterator.Current?.Accept(visitor);
            return visitor;
        }

        public static Dictionary<string, List<DocumentRange>> CharacterRanges(this Document document) => document.Visit(new CharacterStyleVisitor(document)).Ranges;

        public static CharacterPropertiesBase[] ReplaceStyles(this Document document, IDocumentStyle replacement, Document defaultPropertiesProvider , params IDocumentStyle[] styles){
	        defaultPropertiesProvider ??= document;
	        var replacementStyle = replacement.Get(document,defaultPropertiesProvider);
	        document.BeginUpdate();
	        var characterProperties = document.ReplaceStyles( defaultPropertiesProvider, styles, replacementStyle);
	        document.EndUpdate();
	        return characterProperties;
        }

        private static CharacterPropertiesBase[] ReplaceStyles(this Document document, Document defaultPropertiesProvider, IDocumentStyle[] styles, CharacterPropertiesBase replacementStyle) =>
	        replacementStyle is ParagraphStyle paragraphStyle ? document.ReplaceParagraphStyles( defaultPropertiesProvider, styles, paragraphStyle)
		        : document.ReplaceCharacterStyles( styles, replacementStyle);

        private static CharacterPropertiesBase[] ReplaceCharacterStyles(this Document document, IDocumentStyle[] styles, CharacterPropertiesBase replacementStyle) =>
	        document.CharacterRanges()
		        .Select(pair => (ranges: pair.Value, style: styles.FirstOrDefault(style => style.StyleName == pair.Key)))
		        .Where(_ => _.style != null)
		        .SelectMany(_ => _.ranges.Select(range => (charProps: document.BeginUpdateCharacters(range), _.style)))
		        .Do(_ => {
			        _.charProps.Style = (CharacterStyle) replacementStyle;
			        document.EndUpdateCharacters(_.charProps);
		        })
		        .Select(_ => replacementStyle).ToArray();

        private static CharacterPropertiesBase[] ReplaceParagraphStyles(this Document document, Document defaultPropertiesProvider, IDocumentStyle[] styles, ParagraphStyle paragraphStyle) =>
	        document.Paragraphs.Where(paragraph => styles.Contains(paragraph.Style.ToDocumentStyle(defaultPropertiesProvider))).ToArray()
		        .Do(paragraph => paragraph.Style = paragraphStyle)
		        .Select(paragraph => paragraphStyle).Cast<CharacterPropertiesBase>().ToArray();

        [PublicAPI]
        public static void ReplaceStyles(this Document document, IDocumentStyle replacement,params IDocumentStyle[] styles) => document.ReplaceStyles(replacement,null,styles);

        public static string Name(this CharacterPropertiesBase style) => style is IDocumentStyle documentStyle ? documentStyle.StyleName : (string) style.GetPropertyValue("Name");

        public static byte[] ToByteArray(this Document document, DocumentFormat documentFormat){
	        using var memoryStream = new MemoryStream();
	        document.SaveDocument(memoryStream, documentFormat);
	        return memoryStream.ToArray();
        }

        public static DocumentRange WordFromPosition(this Document document){
	        using var richEditDocumentServer = new RichEditDocumentServer();
	        richEditDocumentServer.LoadDocument(document.ToByteArray(DocumentFormat.OpenXml));
	        richEditDocumentServer.Document.CaretPosition = richEditDocumentServer.Document.CreatePosition(document.CaretPosition.ToInt());
	        var element = richEditDocumentServer.DocumentLayout.GetElement(richEditDocumentServer.Document.CaretPosition, LayoutType.PlainTextBox);
	        return element != null ? document.CreateRange(element.Range.Start, element.Range.Length) : document.CreateRange(0, 0);
        }

        public static int FromPosition(this ParagraphCollection paragraphs, int position) =>
	        paragraphs.Select((paragraph, i) => (paragraph, i))
		        .Where(_ => _.paragraph.Range.Start.ToInt() <= position && position < _.paragraph.Range.End.ToInt())
		        .Select(_ => _.i).FirstOrDefault();

        public static int ParagraphFromPosition(this Document document,int? position=null){
            position ??= document.CaretPosition.ToInt();
            return document.Paragraphs.FromPosition(position.Value);
        }

        public static IDocumentStyle DocumentStyleFromPosition(this Document document,Document defaultPropertiesProvider=null){
	        defaultPropertiesProvider ??= document;
            var position = document.CaretPosition.ToInt();
            var characterStyles = document.CharacterStyles.ToDictionary(style => style.Name(), style => style);
            var characterStyle = document.CharacterRanges()
                .FirstOrDefault(_ => !document.IsDefaultStyle(characterStyles[_.Key],defaultPropertiesProvider) && _.Value.Any(range =>
                    range.Start.ToInt() <= position && range.Start.ToInt() + range.Length > position));
            if (characterStyle.Key != null){
                return document.CharacterStyles.First(style => style.Name() == characterStyle.Key).ToDocumentStyle(defaultPropertiesProvider,true);
            }
            var paragraphFromPosition = document.ParagraphFromPosition();
            return document.Paragraphs[paragraphFromPosition].Style.ToDocumentStyle(defaultPropertiesProvider,true);
        }


        
        public static void DeleteStyles(this Document document, params IDocumentStyle[] documentStyles){
            documentStyles = documentStyles.WhenNotDefaultStyle().ToArray();
            document.BeginUpdate();
            foreach (var documentStyle in documentStyles.Select(style => style.Find(document))){
                if (documentStyle is ParagraphStyle paragraphStyle){
                    document.ParagraphStyles.Delete(paragraphStyle);
                }
                else{
                    document.CharacterStyles.Delete((CharacterStyle) documentStyle);
                }
            }
            document.EndUpdate();
        }

    }
}