using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.XtraRichEdit.API.Native;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests{
	public class DocumentStyleServiceTests:CommonTests{
        [Test][XpandTest()]
        public void Property_Attributes_are_synchronized(){
	        using var application=DocumentStyleManagerModule().Application;
	        application.TypesInfo.FindTypeInfo(typeof(TemplateStyle)).FindMember<TemplateStyle>(style => style.FontSize)
		        .FindAttribute<XafDisplayNameAttribute>().DisplayName.ShouldBe("Size");
        }

        
        [TestCase(DocumentStyleType.Paragraph,0)]
        [TestCase(DocumentStyleType.Character,2)]
        [XpandTest()]
        public void Styles_Count_Should_Be_zero_When_no_style_applied(DocumentStyleType type,int allStyles){
            Document.InsertText(Document.CaretPosition, "test");
            var documentStyles = Document.UsedStyles(type).WhenNotDefaultStyle();
            documentStyles.Count().ShouldBe(0);
            documentStyles = Document.AllStyles(type).WhenNotDefaultStyle();
            documentStyles.Count().ShouldBe(allStyles);
        }

        [TestCase(DocumentStyleType.Paragraph)]
        [TestCase(DocumentStyleType.Character)]
        [XpandTest()]
        public void New_DocumentStyle_Reflects_ParentStyle_Properties(DocumentStyleType type){
	        var parent = CreateNewStyle(type);
	        parent.SetPropertyValue("Name","parent");
	        parent.FontName = "Test";
	        var child = CreateNewStyle(type);
	        child.SetPropertyValue("Name","child");
	        child.SetPropertyValue("Parent",parent);
	        CharacterPropertiesBase next = null;
	        if (type==DocumentStyleType.Paragraph){
		        next = CreateNewStyle(type);
		        child.SetPropertyValue("NextStyle", next);
	        }


	        var documentStyle = child.ToDocumentStyle(Document);

	        documentStyle.StyleName.ShouldBe("child");
            documentStyle.FontName.ShouldBe("Test");
            ((IDocumentStyle) documentStyle).PropeprtiesMap[nameof(documentStyle.FontName)].ShouldBe(parent.Name());
            documentStyle.Parent.ShouldNotBeNull();
            documentStyle.Parent.StyleName.ShouldBe(parent.Name());
            ((IDocumentStyle) documentStyle.Parent).PropeprtiesMap[nameof(documentStyle.FontName)].ShouldBe(parent.Name());

            if (type == DocumentStyleType.Paragraph){
	            documentStyle.Next.ShouldNotBeNull();
	            documentStyle.Next.StyleName.ShouldBe(next.Name());
            }
            
	        
        }

        private CharacterPropertiesBase CreateNewStyle(DocumentStyleType type) =>
	        type == DocumentStyleType.Paragraph ? (CharacterPropertiesBase) Document.ParagraphStyles.CreateNew()
		        : Document.CharacterStyles.CreateNew();


        [TestCase(DocumentStyleType.Character)]
        [TestCase(DocumentStyleType.Paragraph)]
        [XpandTest()]
        public void New_CharacterPropertiesBase_Reflects_ParentStyle_Properties(DocumentStyleType documentStyleType){
	        var parent = new DocumentStyle{FontName = "Test", StyleName = "parent",DocumentStyleType = documentStyleType};
	        ((IDocumentStyle) parent).PropeprtiesMap[nameof(IDocumentStyle.FontName)]=parent.StyleName;

	        var child = new DocumentStyle{StyleName = "child", Parent = parent,DocumentStyleType = documentStyleType};
	        ((IDocumentStyle) child).PropeprtiesMap[nameof(IDocumentStyle.FontName)]=parent.StyleName;

	        var next = new DocumentStyle(){StyleName = "next",DocumentStyleType = documentStyleType};
	        ((IDocumentStyle) next).PropeprtiesMap[nameof(IDocumentStyle.FontName)]=next.StyleName;
	        child.Next=next;

	        var style = Document.CreateNewStyle(child);

	        style.Name().ShouldBe("child");
            style.FontName.ShouldBeNull();
            var parentStyle = (CharacterPropertiesBase)style.GetPropertyValue("Parent");
            parentStyle.ShouldNotBeNull();
            parentStyle.Name().ShouldBe(parent.StyleName);
            parentStyle.FontName.ShouldBe("Test");
            
            if (documentStyleType==DocumentStyleType.Paragraph){
	            var nextStyle = (CharacterPropertiesBase)style.GetPropertyValue("NextStyle");
	            nextStyle.ShouldNotBeNull();
	            nextStyle.Name().ShouldBe(next.StyleName);
            }
        }
        
        [TestCase(DocumentStyleType.Character)]
        [TestCase(DocumentStyleType.Paragraph)]
        [XpandTest()]
        public void New_DocumentStyle_Default_Properties_Should_reflect_To_DefaultPropertiesProvider(DocumentStyleType type){
	        
	        RichEditDocumentServer.CreateNewDocument();
	        RichEditDocumentServer.Document.DefaultCharacterProperties.FontSize = 30;

	        var styles =type==DocumentStyleType.Paragraph? Document.ParagraphStyles:Document.CharacterStyles.Cast<CharacterPropertiesBase>();
	        var documentStyle = styles.Select(style => style.ToDocumentStyle(RichEditDocumentServer.Document)).First();
	        
	        documentStyle.FontName.ShouldNotBeNull();
	        documentStyle.FontSize.ShouldBe(30);
	        if (type == DocumentStyleType.Paragraph){
		        documentStyle.SuppressHyphenation.ShouldNotBeNull();
	        }
        }

        [Test][XpandTest()]
        public void CreateNewStyle_Updates_DefaultProperties(){
	        RichEditDocumentServer.CreateNewDocument();
	        RichEditDocumentServer.Document.DefaultCharacterProperties.FontSize = 30;
	        RichEditDocumentServer.Document.DefaultParagraphProperties.SuppressHyphenation = true;

	        var nativeStyle = Document.CreateNewStyle(new DocumentStyle(){StyleName = "test"},RichEditDocumentServer.Document);

            nativeStyle.FontSize.ShouldBeNull();
            Document.DefaultCharacterProperties.FontSize.ShouldBe(30);
            Document.DefaultParagraphProperties.SuppressHyphenation.ShouldNotBeNull();
            Document.DefaultParagraphProperties.SuppressHyphenation.Value.ShouldBeTrue();
        }


        [TestCase(DocumentStyleType.Paragraph,2)]
        [TestCase(DocumentStyleType.Character,4)]
        [XpandTest()]
        public void Get_DocumentStyles(DocumentStyleType type,int allStyles){
            var styles = Document.NewDocumentStyle( 2,type).ToArray();

            var documentStyles = Document.UsedStyles(type).WhenNotDefaultStyle().ToArray();
            documentStyles.Length.ShouldBe(2);
            documentStyles.Select((style, i) =>  style.DocumentStyleType==type&&style.StyleName==styles[i].Name()).Count().ShouldBe(2);
            
            documentStyles = Document.AllStyles(type).WhenNotDefaultStyle().ToArray();
            documentStyles.Length.ShouldBe(allStyles);
            documentStyles.Select((style, i) => style.DocumentStyleType==type&&style.StyleName==styles[i].Name()).ShouldNotBeNull();
            documentStyles.Select(style => style.FontName).All(s => s!=null).ShouldBe(true);
        }

        [TestCase(DocumentStyleType.Character)]
        [TestCase(DocumentStyleType.Paragraph)]
        [XpandTest()]
        public void Used_Styles_Contain_Parent_And_Next(DocumentStyleType type){
	        var style = Document.NewDocumentStyle( 1,type).ToArray().First();
	        var parentStyle =type==DocumentStyleType.Paragraph? (CharacterPropertiesBase) Document.ParagraphStyles.CreateNew():Document.CharacterStyles.CreateNew();
	        parentStyle.SetPropertyValue("Name", "parent");
	        style.SetPropertyValue("Parent", parentStyle);
            if (type==DocumentStyleType.Paragraph){
	            var nextStyle = Document.ParagraphStyles.CreateNew();
	            nextStyle.Name = "next";
	            nextStyle.SetPropertyValue("NextStyle", nextStyle);
	            style.SetPropertyValue("NextStyle", nextStyle);
            }

	        var usedStyles = Document.UsedStyles(type).ToArray();

	        usedStyles.Count(documentStyle => new[]{"parent","next",style.Name()}.Contains(documentStyle.StyleName)).ShouldBe(type==DocumentStyleType.Paragraph?3:2);
            usedStyles.All(documentStyle => documentStyle.Used).ShouldBeTrue();
        }

        
        
        [TestCase(DocumentStyleType.Paragraph)]
        [TestCase(DocumentStyleType.Character)]
        [XpandTest()]
        public void Delete_Styles(DocumentStyleType type){
            var paragraphStyle = Document.NewDocumentStyle(1,type).First();
            var documentStyles = Document.UsedStyles(type).ToArray();
            
            Document.DeleteStyles(documentStyles);
            
            documentStyles = Document.UsedStyles(type).WhenNotDefaultStyle().ToArray();
            documentStyles.Length.ShouldBe(0);
            documentStyles = Document.AllStyles(type).Where(style => style.IsDeleted).ToArray();
            documentStyles.Length.ShouldBe(1);
            var documentStyle = documentStyles.FirstOrDefault(style => style.DocumentStyleType==type&&style.StyleName==paragraphStyle.Name());
            documentStyle.ShouldNotBeNull();
            documentStyle.IsDeleted.ShouldBeTrue();
        }

        
        [TestCase(DocumentStyleType.Paragraph)]
        [TestCase(DocumentStyleType.Character)]
        [XpandTest()]
        public void Replace_many_styles_with_one(DocumentStyleType type){
            Document.NewDocumentStyle(2, type);
            var styles = Document.UsedStyles().WhenNotDefaultStyle().ToArray();
        
            var replacementStyle = new DocumentStyle(){StyleName = "replacement",FontSize = 22,FontName = "Verdana",DocumentStyleType = type};
            Document.ReplaceStyles(replacementStyle,styles);
        
            styles = Document.UsedStyles(type).WhenNotDefaultStyle().ToArray();
            styles.Length.ShouldBe(1);
            replacementStyle.ShouldBe(styles.First());
            replacementStyle = (DocumentStyle) styles.First();
            replacementStyle.Used.ShouldBe(true);
            styles = Document.AllStyles(type).WhenNotDefaultStyle().Where(style => style.Used).ToArray();
            styles.Length.ShouldBe(1);
            styles.FirstOrDefault(style => style.DocumentStyleType==type&&style.StyleName==replacementStyle.StyleName).ShouldNotBeNull();
        }


        [TestCase(10,"1")]
        [TestCase(7,"1")]
        [TestCase(6,"1")]
        [TestCase(5,"0")]
        [TestCase(1,"0")]
        [TestCase(0,"0")]
        [XpandTest()]
        public void ParagraphStyleFromPosition(int position,string styleName){
            Document.NewDocumentStyle(2, DocumentStyleType.Paragraph);
            Document.CaretPosition=Document.CreatePosition(position);

            var documentStyle = Document.DocumentStyleFromPosition();
            
            documentStyle.DocumentStyleType.ShouldBe(DocumentStyleType.Paragraph);
            documentStyle.StyleName.ShouldBe($"{TestExtensions.PsStyleName}{styleName}");
            documentStyle.Used.ShouldBe(true);
        }

        [TestCase(2,1,TestExtensions.CsStyleName+"0",DocumentStyleType.Character)]
        [XpandTest()]
        public void CharacterStyleFromPosition(int start,int length,string styleName,DocumentStyleType documentStyleType){
            Document.NewDocumentStyle(1, DocumentStyleType.Paragraph);
            Document.NewDocumentStyle(1, DocumentStyleType.Character);
            Document.CaretPosition = Document.CreatePosition(6);
            
            var style = Document.DocumentStyleFromPosition();
        
            style.StyleName.ShouldBe(styleName);
            style.DocumentStyleType.ShouldBe(documentStyleType);
        }

        [TestCase(0,1,0)]
        [TestCase(5,1,0)]
        [TestCase(16,2,1)]
        [Theory]
        [XpandTest()]
        public void ParagraphFromPosition(int position,int paragraphCount,int paragraph){
            Document.NewDocumentStyle(paragraphCount, DocumentStyleType.Paragraph);
            Document.CaretPosition = Document.CreatePosition(position);

            var paragraphFromPosition = Document.ParagraphFromPosition();

            paragraphFromPosition.ShouldBe(paragraph);
        }

        [TestCase("test other",6,5,10)]
        [TestCase("test",1,0,4)]
        [XpandTest()]
        public void WordFromPosition(string word,int position,int start,int end){
            Document.AppendText(word);
            Document.CaretPosition = Document.CreatePosition(position);

            var wordFromPosition = Document.WordFromPosition();
            
            wordFromPosition.Start.ToInt().ShouldBe(start);
            wordFromPosition.End.ToInt().ShouldBe(end);
        }

        [Test]
        [XpandTest()]
        public void ApplyParagraphStyleToPosition(){
            var existingStyle = Document.NewDocumentStyle(2, DocumentStyleType.Paragraph,true).Last().ToDocumentStyle(Document);
            var start = Document.Paragraphs.Last().Range.Start.ToInt()+1;
            Document.CaretPosition=Document.CreatePosition(start);
        
            Document.ApplyStyle(existingStyle);
        
            Document.DocumentStyleFromPosition().ShouldBe(existingStyle);
            
        }
        [Test][XpandTest()]
        public void ApplyCharacterStyleToPosition(){
            var existingStyle = Document.NewDocumentStyle(2, DocumentStyleType.Character,true).Last().ToDocumentStyle(Document);
            var start = Document.Paragraphs.Last().Range.Start.ToInt()+1;
            Document.CaretPosition=Document.CreatePosition(start);
        
            Document.ApplyStyle(existingStyle);
        
            Document.DocumentStyleFromPosition().ShouldBe(existingStyle);
            
        }

        [TestCase(DocumentStyleType.Character)]
        [TestCase(DocumentStyleType.Paragraph)]
        [XpandTest()]
        public void EnsureStyle_Does_not_update_style_if_used(DocumentStyleType type){
	        var nativeStyle = Document.NewDocumentStyle(1, type).First();
	        nativeStyle.FontName = "test";
	        var documentStyle = new DocumentStyle(){StyleName = nativeStyle.Name(),DocumentStyleType = type};

	        var ensureStyle = documentStyle.Ensure(Document);

            ensureStyle.ShouldBeFalse();

            documentStyle.Find(Document).FontName.ShouldBe("test");
        }

        [TestCase(DocumentStyleType.Character)]
        [TestCase(DocumentStyleType.Paragraph)]
        [XpandTest()]
        public void EnsureStyle_adds_style_if_not_exists(DocumentStyleType type){
	        var documentStyle = new DocumentStyle(){StyleName = "test",DocumentStyleType = type};

	        var ensureStyle = documentStyle.Ensure(Document);

	        ensureStyle.ShouldBeTrue();

	        documentStyle.Find(Document).Name().ShouldBe("test");
        }

        [TestCase(DocumentStyleType.Character)]
        [TestCase(DocumentStyleType.Paragraph)]
        [XpandTest()]
        public void EnsureStyle_Update_style_if_not_used(DocumentStyleType type){
	        var nativeStyle = Document.NewDocumentStyle(1, type,true).First();
	        nativeStyle.FontName = "test";
	        var documentStyle = new DocumentStyle(){StyleName = nativeStyle.Name(),DocumentStyleType = type};

	        var ensureStyle = documentStyle.Ensure(Document);

	        ensureStyle.ShouldBeTrue();

	        documentStyle.Find(Document).FontName.ShouldBe("test");
        }

    }
}