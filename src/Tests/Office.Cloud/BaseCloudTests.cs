using NUnit.Framework;
using Xpand.TestsLib;
using Task = System.Threading.Tasks.Task;

namespace Xpand.XAF.Modules.Office.Cloud.Tests{
    public abstract class BaseCloudTests:BaseTest{
        [Order(0)]
        public abstract Task Populate_All(string syncToken);
        [Order(1)]
        public abstract Task Populate_Modified();
        [Order(1)]
        public abstract Task Create_Entity_Container_When_Not_Exist();
        
        
        
    }
}