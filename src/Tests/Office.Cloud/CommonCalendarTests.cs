using System.Threading.Tasks;


namespace Xpand.XAF.Modules.Office.Cloud.Tests{
    public abstract class CommonCalendarTests:CommonCloudTests{
        
        
        public abstract Task Map_Two_New_Events();
        
        public abstract Task Map_Existing_Event_Two_Times();
        
        public abstract Task Delete_Two_Events();
        
        public abstract Task Delete_Local_Event_Resource();
        public abstract Task Delete_Cloud_Event();
        public abstract Task Insert_Cloud_Event();
        public abstract Task Update_Cloud_Event();

        
        public abstract Task Customize_Two_New_Event();

        
        public abstract Task Customize_Map_Existing_Event_Two_Times();

        
        public abstract Task Customize_Delete_Two_Events(bool handleDeletion);

        
    }
}