namespace Xpand.XAF.Modules.Reactive.Rest {
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "XAF0016:NonPersistentBaseObject and NonPersistentLiteObject descendants must be decorated with the DomainComponent attribute", Justification = "<Pending>")]
	public abstract class NonPersistentBaseObject:Xpand.Extensions.XAF.NonPersistentObjects.NonPersistentBaseObject {
        public const int AppLifeCycle = DailyPoll*10000;
        public const int DailyPoll = HourlyPoll*24;
        public const int HourlyPoll = 60*60;
    }

}