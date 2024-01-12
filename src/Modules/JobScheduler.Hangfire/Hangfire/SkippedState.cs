using System.Collections.Generic;
using Hangfire.States;

public class SkippedState(string reason = null) : IState {
    public string Name => "Skipped";
    public string Reason { get; } = reason;
    public bool IsFinal => true;
    public bool IgnoreJobLoadException => true;

    public Dictionary<string, string> SerializeData() => new();
}