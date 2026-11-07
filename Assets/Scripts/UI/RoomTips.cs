using System;
using TMPro;
using UnityEngine;

public sealed class RoomTips : MonoBehaviour
{
    [SerializeField] private TMP_Text tipText;
    [SerializeField, Min(1f)] private float interval = 30f;
    [SerializeField] private string[] tips =
    {
        "Press Esc to pause. During the match intro, Esc skips it; resolution dialogs use Esc first.",
        "Recall camera presets with 1–4. Hold Shift + 1–4 to replace a preset for this Room session.",
        "Press E to show or hide the side panel when no dialog is blocking play.",
        "Use the arrow keys to pan the camera across the pitch.",
        "Hold the left mouse button and drag to pan. A simple click still selects players and hexes.",
        "Hold the right mouse button and drag to orbit the camera.",
        "Scroll, or hold + / -, to zoom in or out.",
        "Hold Ctrl + Shift + S to see shooting hexes at both ends; hold Ctrl + Shift + H for heading hexes.",
        "Press Space over a pitch player or lineup name to inspect their card. Press Space again to close it.",
        "While inspecting a card, camera controls and Esc remain available. CPU decisions and current actions continue."
    };
    private int[] order;
    private int index;
    private float elapsed;
    public int CurrentTipIndex => order == null || order.Length == 0 ? -1 : order[index];
    public float Elapsed => elapsed;

    private void Start()
    {
        order = new int[tips.Length];
        for (int i = 0; i < order.Length; i++) order[i] = i;
        var random = new System.Random(Guid.NewGuid().GetHashCode());
        for (int i = order.Length - 1; i > 0; i--) { int j = random.Next(i + 1); (order[i], order[j]) = (order[j], order[i]); }
        Show();
    }
    private void Update() => Advance(Time.deltaTime);
    public void Advance(float seconds)
    {
        if (order == null || order.Length == 0 || RoomInputOwnership.IntroActive || MatchManager.Instance?.IsGameplayInputBlocked == true) return;
        elapsed += Mathf.Max(0f, seconds);
        if (elapsed < interval) return;
        elapsed %= interval; index = (index + 1) % order.Length; Show();
    }
    private void Show() { if (tipText != null && CurrentTipIndex >= 0) tipText.text = "Tip: " + tips[CurrentTipIndex]; }
}
