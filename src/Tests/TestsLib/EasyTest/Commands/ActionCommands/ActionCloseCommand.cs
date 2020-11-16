﻿using DevExpress.EasyTest.Framework;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.TestsLib.EasyTest.Commands.ActionCommands{
    public class ActionCloseCommand:EasyTestCommand{
	    private readonly bool _optional;
	    public const string Name = "ActionOK";

        public ActionCloseCommand(bool optional=true){
	        _optional = optional;
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            if (adapter.GetTestApplication().Platform()==Platform.Web){
                var command = new ActionCommand("OK"){SuppressExceptions = _optional};
                command.Execute(adapter);
            }
            else{
                var command = new ActionCommand("Close"){SuppressExceptions = _optional};
                command.Execute(adapter);
            }
	        
            
        }
    }
}