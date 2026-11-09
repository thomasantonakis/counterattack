#if UNITY_EDITOR
using UnityEngine;

// Real MatchManager behavior with only scene bootstrap suppressed. Tests explicitly
// wire their owned board and activate this host for coroutine/panel continuations.
public sealed class DecisionTimingHostedMatch : MatchManager
{
    private void Awake() { }
    private void Start() { }
    private void Update() { }
}
#endif
