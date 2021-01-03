using System.Text.RegularExpressions;
using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.Common.EasyTest.Commands.ActionCommands{
	public class CheckActionToolTip:EasyTestCommand{
		private readonly (string caption, string tooltipMatch)[] _pairs;

		public CheckActionToolTip(params  (string caption,string tooltipMatch)[] pairs){
			_pairs = pairs;
		}

		protected override void ExecuteCore(ICommandAdapter adapter){
			foreach (var pair in _pairs){
				var testControl = adapter.CreateTestControl(TestControlType.Action, pair.caption);
				var input = testControl.GetInterface<IControlHint>().Hint;
				if(!Regex.IsMatch(input,pair.tooltipMatch))
					throw new TestException($"{input} does not match {pair.tooltipMatch}");
			}
			
		}
	}
}