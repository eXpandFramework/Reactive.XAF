using System;
using System.Linq.Expressions;
using DevExpress.EasyTest.Framework;
using DevExpress.EasyTest.Framework.Commands;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.TestsLib.Common.EasyTest.Commands{
    public class EditorActionNewCommand<T> : EditorActionNewCommand{
        public EditorActionNewCommand(Expression<Func<T,object>> editorSelector):base(editorSelector.MemberExpressionCaption()){

        }
    }

    public class EditorActionNewCommand:EasyTestCommand{
        private readonly string _editor;
        public const string Name = "EditorActionNew";

        public EditorActionNewCommand(string editor){
            _editor = editor;
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            var executeEditorActionCommand = new ExecuteEditorActionCommand{
                Parameters = {MainParameter = new MainParameter(_editor.CompoundName()), ExtraParameter = new MainParameter("New")}
            };
            executeEditorActionCommand.Execute(adapter);

        }
    }
}