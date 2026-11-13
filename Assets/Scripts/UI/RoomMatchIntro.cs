using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RoomIntroSkipLatch
{
    private bool held;
    private int releaseFrame = -1;
    public bool IsConsuming(int frame) => held || frame <= releaseFrame;
    public void Begin(bool escapeHeld, bool pointerHeld, int frame)
    { held = escapeHeld || pointerHeld; releaseFrame = frame; }
    public void Observe(bool escapeHeld, bool pointerHeld, int frame)
    { if (held && !escapeHeld && !pointerHeld) { held = false; releaseFrame = frame; } }
    public void Clear() { held = false; releaseFrame = -1; }
}

[DefaultExecutionOrder(-500)]
public sealed class RoomMatchIntro : MonoBehaviour
{
    public static RoomMatchIntro Instance { get; private set; }
    [SerializeField] private CameraController cameraController;
    [SerializeField] private GameObject introUI;
    [SerializeField] private TMP_Text teamNames;
    [SerializeField] private Button skipButton;
    [SerializeField] private CanvasGroup[] normalHud;
    [Tooltip("Opening, reveal, broad shot. Final destination is adaptive Preset 1.")]
    [SerializeField] private Transform[] pathControls;
    [SerializeField, Min(1f)] private float duration = 15f;
    [SerializeField, Min(1f)] private float readinessTimeout = 15f;
    private (float alpha, bool interactable, bool blocks)[] hudStates;
    private bool hidden;
    private readonly RoomIntroSkipLatch skipLatch = new();
    public bool IsHoldingMatch { get; private set; }
    public bool IsConsumingSkipRelease => skipLatch.IsConsuming(Time.frameCount);

    private void Awake()
    {
        if (CounterAttack.Online.OnlineRoomSession.Current != null) { enabled = false; return; }
        Instance = this;
        IsHoldingMatch = ApplicationManager.Instance != null && ApplicationManager.Instance.ConsumeDraftRoomIntro();
        if (introUI != null) introUI.SetActive(false);
        if (IsHoldingMatch) HideHud();
        if (skipButton != null) skipButton.onClick.AddListener(Skip);
    }

    private IEnumerator Start()
    {
        if (!IsHoldingMatch) yield break;
        float deadline = Time.realtimeSinceStartup + readinessTimeout;
        while (!Ready())
        {
            if (!IsHoldingMatch) yield break;
            if (Time.realtimeSinceStartup >= deadline) { Finish(); yield break; }
            yield return null;
        }
        if (MatchManager.Instance.gameData.runtimeSnapshot != null || introUI == null || teamNames == null || skipButton == null || pathControls == null || pathControls.Length < 3)
        { Finish(); yield break; }
        var settings = MatchManager.Instance.gameData.gameSettings;
        if (teamNames != null) teamNames.gameObject.SetActive(false);
        introUI.SetActive(true);
        float elapsed = 0f;
        while (IsHoldingMatch && elapsed < duration)
        {
            ApplyPath(elapsed / duration * 15f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (IsHoldingMatch) Finish();
    }

    private bool Ready()
    {
        var match = MatchManager.Instance;
        if (match?.gameData?.gameSettings == null || match.hexGrid?.IsGridInitialized() != true
            || match.playerTokenManager == null || match.playerTokenManager.allTokens.Count < 22
            || match.ball?.GetCurrentHex() == null || cameraController == null || !cameraController.IsReady) return false;
        int home = 0, away = 0;
        foreach (PlayerToken token in match.playerTokenManager.allTokens)
        {
            if (token == null || !token.isPlaying) continue;
            if (token.GetCurrentHex() == null) return false;
            if (token.isHomeTeam) home++; else away++;
        }
        if (home < 11 || away < 11) return false;
        var crowd = FindAnyObjectByType<StadiumCrowdController>();
        var lines = FindAnyObjectByType<PitchLines>();
        return crowd != null && crowd.HasAppliedColours && lines != null && lines.HasAppliedWeather;
    }

    public void ApplyPath(float seconds)
    {
        if (cameraController == null || pathControls == null || pathControls.Length < 3) return;
        cameraController.GetAdaptivePresetPose(1, out Vector3 endPosition, out Quaternion endRotation);
        int from = seconds < 4f ? 0 : seconds < 10f ? 1 : 2;
        float t = from == 0 ? seconds / 4f : from == 1 ? (seconds - 4f) / 6f : (seconds - 10f) / 5f;
        t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
        Transform start = pathControls[from];
        Transform next = from < 2 ? pathControls[from + 1] : null;
        if (start == null || (from < 2 && next == null)) { Finish(); return; }
        Vector3 position = Vector3.Lerp(start.position, next != null ? next.position : endPosition, t);
        // Tilt before climbing above the continuous bowl wall, keeping the reveal smooth.
        if (from == 1) position.y = Mathf.Lerp(start.position.y, next.position.y, t * t * t);
        cameraController.ApplyPresentationPose(position,
            Quaternion.Slerp(start.rotation, next != null ? next.rotation : endRotation, t), from < 2 ? 0f : Mathf.Lerp(0f, cameraController.GetAdaptiveProjectionShift(), t));
    }

    private void Update()
    {
        skipLatch.Observe(Input.GetKey(KeyCode.Escape), Input.GetMouseButton(0), Time.frameCount);
        if (IsHoldingMatch && Input.GetKeyDown(KeyCode.Escape)) Skip();
    }

    public void Skip()
    {
        if (!IsHoldingMatch) return;
        skipLatch.Begin(Input.GetKey(KeyCode.Escape), Input.GetMouseButton(0), Time.frameCount);
        Finish();
    }

    private void Finish()
    {
        cameraController?.SetCameraToPreset(1);
        IsHoldingMatch = false;
        if (introUI != null) introUI.SetActive(false);
        RestoreHud();
    }

    private void HideHud()
    {
        if (normalHud == null) return;
        hudStates = new (float, bool, bool)[normalHud.Length];
        for (int i = 0; i < normalHud.Length; i++)
        {
            var group = normalHud[i]; if (group == null) continue;
            hudStates[i] = (group.alpha, group.interactable, group.blocksRaycasts);
            group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false;
        }
        hidden = true;
    }
    private void RestoreHud()
    {
        if (!hidden) return;
        for (int i = 0; i < normalHud.Length; i++)
        {
            var group = normalHud[i]; if (group == null) continue;
            group.alpha = hudStates[i].alpha; group.interactable = hudStates[i].interactable; group.blocksRaycasts = hudStates[i].blocks;
        }
        hidden = false;
    }
    private void OnDisable()
    {
        IsHoldingMatch = false; skipLatch.Clear(); RestoreHud();
        if (Instance == this) Instance = null;
        if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
    }

    private void OnDrawGizmosSelected()
    {
        if (pathControls == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < pathControls.Length; i++)
        {
            if (pathControls[i] == null) continue;
            Gizmos.DrawWireSphere(pathControls[i].position, .4f);
            Gizmos.DrawRay(pathControls[i].position, pathControls[i].forward * 2f);
            if (i > 0 && pathControls[i - 1] != null) Gizmos.DrawLine(pathControls[i - 1].position, pathControls[i].position);
        }
    }
}
