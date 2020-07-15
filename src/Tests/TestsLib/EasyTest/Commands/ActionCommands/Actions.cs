using System;
using System.ComponentModel;

namespace Xpand.TestsLib.EasyTest.Commands.ActionCommands{
    public enum Actions{
        Refresh,
        New,
        Save,
        [DefaultValue("Save and Close")]
        SaveAndClose,
        Close,
        [Obsolete("use the ActionDeleteCommand")]
        Delete,
        Cancel
    }
}