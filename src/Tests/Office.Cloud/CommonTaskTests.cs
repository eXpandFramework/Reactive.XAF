using System.Threading.Tasks;

using NUnit.Framework;

namespace Xpand.XAF.Modules.Office.Cloud.Tests{
    public abstract class CommonTaskTests:CommonCloudTests{
        
        [Order(2)]
        public abstract Task Map_Two_New_Tasks(DevExpress.Persistent.Base.General.TaskStatus projectTaskStatus,string taskStatus);
        [Order(3)]
        public abstract Task Customize_Two_New_Tasks();
        [Order(4)]
        public abstract Task Map_Existing_Two_Times(DevExpress.Persistent.Base.General.TaskStatus projectTaskStatus,string taskStatus);
        [Order(5)]
        public abstract Task Customize_Existing_Two_Times();
        [Order(6)]
        public abstract Task Delete_Two_Tasks();
        
        [Order(7)]
        public abstract Task Customize_Delete_Two_Tasks(bool handleDeletion);
        
        
        
    }
}