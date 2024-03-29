﻿using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;

namespace Xpand.TestsLib.EasyTest.Commands {
    public class LoginCommand : EasyTestCommand {
        protected override void ExecuteCore(ICommandAdapter adapter) {
            new FillObjectViewCommand(("User Name", "Admin")).Execute(adapter);
            new ActionCommand("Log In").Execute(adapter);
        }
    }
}