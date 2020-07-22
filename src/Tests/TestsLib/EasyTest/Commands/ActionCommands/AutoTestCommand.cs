using System;
using System.Linq;
using System.Text.RegularExpressions;
using DevExpress.EasyTest.Framework;
using DevExpress.EasyTest.Framework.Commands;

namespace Xpand.TestsLib.EasyTest.Commands.ActionCommands{
    public class AutoTestCommand:EasyTestCommand{
        private readonly string _excludeMatch;

        private readonly string[] _navigationControlPossibleNames = { "ViewsNavigation.Navigation", "Navigation", "navBarControl", "accordionControl" };

        public AutoTestCommand(string excludeMatch=null){
            _excludeMatch = excludeMatch;
        }

        private ITestControl GetNavigationTestControl(ICommandAdapter adapter) {
            string controlNames = "";
            for(int i = 0; i < _navigationControlPossibleNames.Length; i++) {
                if(adapter.IsControlExist(TestControlType.Action, _navigationControlPossibleNames[i])) {
                    try {
                        ITestControl testControl = adapter.CreateTestControl(TestControlType.Action, _navigationControlPossibleNames[i]);
                        IGridBase gridBaseInterface = testControl.GetInterface<IGridBase>();
                        int itemsCount = gridBaseInterface.GetRowCount();
                        if(itemsCount > 0) {
                            return testControl;
                        }
                    }
                    catch(WarningException) { }
                }
                controlNames += (i <= _navigationControlPossibleNames.Length) ? _navigationControlPossibleNames[i] + " or " : _navigationControlPossibleNames[i];
            }
            throw new WarningException($"Cannot find the '{controlNames}' control");
        }


        protected override void ExecuteCore(ICommandAdapter adapter){
            string logonCaption="Log In";
            if(adapter.IsControlExist(TestControlType.Action, logonCaption)) {
                adapter.CreateTestControl(TestControlType.Action, logonCaption).GetInterface<IControlAct>().Act(null);
            }

            var navigationTestControl = GetNavigationTestControl(adapter);
            var grid = navigationTestControl.GetInterface<IGridBase>();
            int itemsCount = grid.GetRowCount();
            var namigationItems = Enumerable.Range(0,itemsCount).Select(i => grid.GetCellValue(i, grid.Columns.First())).ToArray();
            for(int i = 0; i < itemsCount; i++) {
                var namigationItem = namigationItems[i];
                if (_excludeMatch==null||!Regex.IsMatch(namigationItem,_excludeMatch)){
                    var testControl = GetNavigationTestControl(adapter);
                    var gridBase = testControl.GetInterface<IGridBase>();
                    var navigationItemName = gridBase.GetCellValue(i, gridBase.Columns.First());
                    var controlAct = testControl.GetInterface<IControlAct>();
                    controlAct.Act(navigationItemName);
                    if(adapter.IsControlExist(TestControlType.Action, "New")) {
                        try {
                            adapter.CreateTestControl(TestControlType.Action, "New").FindInterface<IControlAct>().Act("");
                        }
                        catch(Exception e) {
                            throw new CommandException(
                                $"The 'New' action execution failed. Navigation item: {navigationItemName}\r\nInner Exception: {e.Message}", this.StartPosition);
                        }
                        if(adapter.IsControlExist(TestControlType.Action, "Cancel")) {
                            try {
                                ITestControl cancelActionTestControl = adapter.CreateTestControl(TestControlType.Action, "Cancel");
                                if(cancelActionTestControl.GetInterface<IControlEnabled>().Enabled) {
                                    cancelActionTestControl.FindInterface<IControlAct>().Act(null);
                                }
                            }
                            catch(Exception e) {
                                throw new CommandException(
                                    $"The 'Cancel' action execution failed. Navigation item: {navigationItemName}\r\nInner Exception: {e.Message}", this.StartPosition);
                            }
                        }

                        var command = new OptionalActionCommand{
                            Parameters = {MainParameter = new MainParameter("Yes"), ExtraParameter = new MainParameter()}
                        };
                        command.Execute(adapter);
                    }
                }
            }
        }
    }
}