using System;
using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.PositionInListView.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.PositionInListView.Tests{
    public abstract class PositionInListViewCommonTest:BaseTest{
	    protected IModelPositionInListViewListViewItem ListViewItem;

	    protected PositionInListViewModule PositionInListViewModuleModule(Action<XafApplication> loggingOn=null,params ModuleBase[] modules){
            var positionInListViewModule = Platform.Win.NewApplication<PositionInListViewModule>().AddModule<PositionInListViewModule>(typeof(PIL));
            var xafApplication = positionInListViewModule.Application;
            xafApplication.MockPlatformListEditor();
            xafApplication.Modules.AddRange(modules);
            var modelPositionInListView = xafApplication.Model.ToReactiveModule<IModelReactiveModulesPositionInListView>().PositionInListView;
            ListViewItem = modelPositionInListView.ListViewItems.AddNode<IModelPositionInListViewListViewItem>();
            ListViewItem.ListView = xafApplication.Model.BOModel.GetClass(typeof(PIL)).DefaultListView;
            loggingOn?.Invoke(xafApplication);
            xafApplication.Logon();
            xafApplication.CreateObjectSpace();
            return positionInListViewModule;
        }
    }
}