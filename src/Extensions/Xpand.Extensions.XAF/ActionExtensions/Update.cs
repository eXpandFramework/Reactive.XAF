using System;
using DevExpress.ExpressApp.Actions;

namespace Xpand.Extensions.XAF.ActionExtensions;

public static partial class ActionExtensions {
    public static TAction Update<TAction>(this TAction action, Action<TAction> update) where TAction : ActionBase {
        action.BeginUpdate();
        try {
            update(action);
        }
        finally {
            action.EndUpdate();
        }
        return action;
    }
}