using System.Collections.Generic;
using Hangfire.States;
using Hangfire.Storage;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Hangfire {
    public class ChainJobState : IState {
        public string Name => StateName;
        public string Reason => "Chain Job execution";
        public bool IsFinal => false;
        public bool IgnoreJobLoadException => true;

        public static readonly string StateName = nameof(ChainJobState).Replace("State","");

        public Dictionary<string, string> SerializeData() => new();

        public class Handler : IStateHandler {
            public static readonly string StateStatKey = $"stats:{StateName.ToLower()}";

            public void Apply(ApplyStateContext context, IWriteOnlyTransaction transaction) => transaction.IncrementCounter(StateStatKey);

            public void Unapply(ApplyStateContext context, IWriteOnlyTransaction transaction) => transaction.DecrementCounter(StateStatKey);

            string IStateHandler.StateName => ChainJobState.StateName;
        }
    }
}