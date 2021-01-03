using System;
using System.ComponentModel;

namespace Xpand.TestsLib.Common.EasyTest.Commands.ActionCommands{
    public enum Actions{
        Refresh,
        New,
        Save,
        [DefaultValue("Save and Close")]
        SaveAndClose,
        Close,
        OK,
        [Obsolete("use the ActionDeleteCommand")]
        Delete,
        Cancel
    }
}