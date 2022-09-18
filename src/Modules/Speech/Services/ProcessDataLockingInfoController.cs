using System;
using System.Linq;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.XAF.Modules.Speech.Services {

    public class ProcessDataLockingInfoController:DevExpress.ExpressApp.SystemModule.ProcessDataLockingInfoController{
        protected override DataLockingInfo GetDataLockingInfo() {
            var dataLockingInfo = base.GetDataLockingInfo();
            
            return dataLockingInfo;
        }

        protected override void ProcessDataLockingInfo(DataLockingInfo dataLockingInfo, out bool cancelAction){
            
            if(ObjectSpace is IDataLockingManager{ IsActive: true } && dataLockingInfo.IsLocked) {
                var nonIgnoredInfos = dataLockingInfo.ObjectLockingInfo
                    .Select(info => new ObjectLockingInfo(info.LockedObject,info.CanMerge,info.ServerSideModifiedMemberNames.Where(property =>
                        !info.LockedObject.GetTypeInfo().FindMember(property).FindAttributes<IgnoreDataLockingAttribute>().Any()).ToArray()))
                    .Where(info => info.ServerSideModifiedMemberNames.Any()).ToArray();
        
                dataLockingInfo = new DataLockingInfo(nonIgnoredInfos);
                if (dataLockingInfo.IsLocked){
                    this.CallMethod("ShowDialog",dataLockingInfo);
                    cancelAction = true;
                }
                else{
                    cancelAction = false;
                }
            }
            else {
                cancelAction = false;
            }
            
        }
    }
}