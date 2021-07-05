using JetBrains.Annotations;

namespace Xpand.Extensions.Windows.Manager{
    [PublicAPI]
    public enum MsgBoxResult{
        Abort = 3,
        Cancel = 2,
        Ignore = 5,
        No = 7,
        Ok = 1,
        Retry = 4,
        Yes = 6
    }
}