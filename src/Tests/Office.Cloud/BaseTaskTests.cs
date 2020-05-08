using System.Threading.Tasks;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Xpand.XAF.Modules.Office.Cloud.Tests{
    public abstract class BaseTaskTests:BaseCloudTests{
        
        [UsedImplicitly][Order(2)]
        public abstract Task Map_Two_New_Tasks(DevExpress.Persistent.Base.General.TaskStatus projectTaskStatus,string taskStatus);
        [UsedImplicitly][Order(3)]
        public abstract Task Customize_Two_New_Tasks();
        [UsedImplicitly][Order(4)]
        public abstract Task Map_Existing_Two_Times(DevExpress.Persistent.Base.General.TaskStatus projectTaskStatus,string taskStatus);
        [UsedImplicitly][Order(5)]
        public abstract Task Customize_Existing_Two_Times();
        [UsedImplicitly][Order(6)]
        public abstract Task Delete_Two_Tasks();
        
        [UsedImplicitly][Order(7)]
        public abstract Task Customize_Delete_Two_Tasks(bool handleDeletion);
        
        
        
    }
}