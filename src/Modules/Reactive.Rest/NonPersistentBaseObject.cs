namespace Xpand.XAF.Modules.Reactive.Rest {
    public abstract class NonPersistentBaseObject:Xpand.Extensions.XAF.NonPersistentObjects.NonPersistentBaseObject {
        public const int AppLifeCycle = DailyPoll*10000;
        public const int DailyPoll = HourlyPoll*24;
        public const int HourlyPoll = 60*60;
    }

}