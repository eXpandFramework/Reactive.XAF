using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Fasterflect;
using Hangfire;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire {
    public class CronExpressionModuleUpdater:ModuleUpdater {
        public CronExpressionModuleUpdater(IObjectSpace objectSpace, Version currentDBVersion) : base(objectSpace, currentDBVersion) {
        }

        public override void UpdateDatabaseAfterUpdateSchema() {
            base.UpdateDatabaseAfterUpdateSchema();
            if (!ObjectSpace.Any<CronExpression>()) {
                var expressions = new[]{nameof(Cron.Never),nameof(Cron.Minutely),nameof(Cron.Hourly),nameof(Cron.Daily),nameof(Cron.Monthly),nameof(Cron.Yearly)}
                    .Select(s => (name:s,value:$"{typeof(Cron).Method(s,Type.EmptyTypes,Flags.StaticPublic).Call(null)}"));
                foreach (var expression in expressions) {
                    var cronExpression = ObjectSpace.CreateObject<CronExpression>();
                    cronExpression.Name = expression.name;
                    cronExpression.Expression = expression.value;
                }
                ObjectSpace.CommitChanges();
            }
        }
    }
}