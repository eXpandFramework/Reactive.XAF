using System.Reflection;
using DevExpress.XtraGrid;
using Xpand.TestsLib.Common;

namespace Xpand.TestsLib {
    public abstract class BaseTest:CommonTest {
        static BaseTest() {
            Assembly.LoadFile(typeof(GridControl).Assembly.Location);
        }
    }
}