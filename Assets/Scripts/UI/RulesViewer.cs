using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Controls an edit-mode-authored rulebook. Never constructs visual objects.</summary>
public sealed class RulesViewer : MonoBehaviour, IPointerClickHandler
{
    [Serializable] public sealed class Entry
    {
        public TMP_Text text;
        public RectTransform rect;
        public string anchor;
        public string section;
        public Button contentsButton;
        public Button resultButton;
        public bool heading;
    }

    public GameObject surface;
    public ScrollRect article;
    public ScrollRect resultsScroll;
    public TMP_InputField search;
    public TMP_Text matchCount;
    public Button closeButton, backButton, forwardButton, nextButton, previousButton, clearButton;
    public Entry[] entries = Array.Empty<Entry>();
    public static bool IsOpen { get; private set; }
    public static bool ReserveEscape => IsOpen || escapeReleaseFrame == Time.frameCount;
    private static int escapeReleaseFrame = -1;
    private static string savedQuery = "";
    private static float savedPosition = 1f;
    private static readonly List<float> History = new();
    private static int historyIndex = -1;
    private List<RulesDocument.Match> matches = new();
    private string[] plain;
    private int activeMatch = -1;
    private GameObject priorSelection;
    private int highlightedContents = -1;
    private float observedPosition;
    private float scrollSettlesAt;
    private bool pendingScroll;
    private bool restoreSearchFocus;
    private bool consumeSearchEscapeRelease;

    void Awake()
    {
        closeButton.onClick.AddListener(Close);
        backButton.onClick.AddListener(() => TraverseHistory(-1));
        forwardButton.onClick.AddListener(() => TraverseHistory(1));
        nextButton.onClick.AddListener(() => StepMatch(1));
        previousButton.onClick.AddListener(() => StepMatch(-1));
        clearButton.onClick.AddListener(() => search.text = "");
        search.onValueChanged.AddListener(Search);
        search.restoreOriginalTextOnEscape = false;
        search.onSubmit.AddListener(_ =>
        {
            if (!IsOpen) return;
            nextButton.onClick.Invoke();
            restoreSearchFocus = true;
        });
        search.onEndEdit.AddListener(_ =>
        {
            if (!IsOpen || !search.wasCanceled || string.IsNullOrEmpty(search.text)) return;
            clearButton.onClick.Invoke();
            consumeSearchEscapeRelease = true;
            restoreSearchFocus = true;
        });
        plain = new string[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            int index = i;
            if (entries[i].contentsButton != null) entries[i].contentsButton.onClick.AddListener(() => Jump(index));
            entries[i].resultButton.onClick.AddListener(() => SelectMatch(matches.FindIndex(m => m.block == index)));
        }
    }

    public static void OpenRules()
    {
        var viewer = FindObjectsByType<RulesViewer>(FindObjectsInactive.Include)
            .FirstOrDefault(v => v.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        if (viewer == null) { Debug.LogError("Rules viewer is not authored in this scene."); return; }
        viewer.Open();
    }

    public void Open()
    {
        if (IsOpen) return;
        consumeSearchEscapeRelease = false;
        restoreSearchFocus = false;
        priorSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        // Rules is an inspection overlay. A caller opening it from Pause retains
        // that existing pause; the instruction-box shortcut leaves gameplay live.
        surface.SetActive(true);
        IsOpen = true;
        Canvas.ForceUpdateCanvases();
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i].text.ForceMeshUpdate();
            plain[i] = entries[i].text.GetParsedText();
        }
        search.SetTextWithoutNotify(savedQuery);
        Search(savedQuery);
        Canvas.ForceUpdateCanvases();
        article.verticalNormalizedPosition = savedPosition;
        observedPosition = article.verticalNormalizedPosition;
        pendingScroll = false;
        if (historyIndex < 0) AppendHistory(observedPosition);
        UpdateHistoryButtons();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
    }

    public void Close()
    {
        if (!surface.activeSelf) return;
        savedPosition = article.verticalNormalizedPosition;
        CaptureScroll(true);
        savedQuery = search.text;
        surface.SetActive(false);
        IsOpen = false;
        restoreSearchFocus = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(priorSelection);
    }

    void OnDisable()
    {
        if (surface != null && surface.activeSelf)
        {
            savedPosition = article.verticalNormalizedPosition;
            savedQuery = search.text;
            IsOpen = false;
        }
    }

    void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            escapeReleaseFrame = Time.frameCount;
            if (consumeSearchEscapeRelease) consumeSearchEscapeRelease = false;
            else if (!string.IsNullOrEmpty(search.text)) clearButton.onClick.Invoke();
            else Close();
            return;
        }
        CaptureScroll(false);
        UpdateContents();
        UpdateHistoryButtons();
    }

    void LateUpdate()
    {
        if (!restoreSearchFocus || !IsOpen) return;
        restoreSearchFocus = false;
        search.ActivateInputField();
    }

    public void OnPointerClick(PointerEventData pointer)
    {
        if (!IsOpen || pointer.button != PointerEventData.InputButton.Left) return;
        var camera = article.GetComponentInParent<Canvas>().worldCamera;
        if (!RectTransformUtility.RectangleContainsScreenPoint(article.viewport, pointer.position, camera)) return;
        foreach (Entry entry in entries)
        {
            int link = TMP_TextUtilities.FindIntersectingLink(entry.text, pointer.position, camera);
            if (link < 0) continue;
            Navigate(entry.text.textInfo.linkInfo[link].GetLinkID());
            return;
        }
    }

    public void Navigate(string target)
    {
        if (Uri.TryCreate(target, UriKind.Absolute, out Uri url))
        {
            if (url.Scheme == "https" || url.Scheme == "http") Application.OpenURL(target);
            return;
        }
        int index = Array.FindIndex(entries, e => e.anchor == target);
        if (index >= 0) Jump(index);
        else Debug.LogWarning("Unknown rules destination: " + target);
    }

    void AppendHistory(float position)
    {
        if (historyIndex < 0 || Mathf.Abs(History[historyIndex] - position) > .0001f)
        {
            if (historyIndex + 1 < History.Count) History.RemoveRange(historyIndex + 1, History.Count - historyIndex - 1);
            History.Add(position);
            historyIndex = History.Count - 1;
        }
        observedPosition = position;
        pendingScroll = false;
        UpdateHistoryButtons();
    }

    // Wheel/drag reading is navigation too. Commit one checkpoint when scrolling
    // settles, and flush it before a button click so Back works immediately.
    void CaptureScroll(bool flush)
    {
        float position = article.verticalNormalizedPosition;
        if (Mathf.Abs(position - observedPosition) > .0001f)
        {
            pendingScroll = true;
            observedPosition = position;
            scrollSettlesAt = Time.unscaledTime + .25f;
        }
        if (pendingScroll && (flush || Time.unscaledTime >= scrollSettlesAt)) AppendHistory(position);
    }

    void UpdateHistoryButtons()
    {
        backButton.interactable = pendingScroll || historyIndex > 0;
        forwardButton.interactable = !pendingScroll && historyIndex >= 0 && historyIndex < History.Count - 1;
    }

    void Jump(int index, bool remember = true)
    {
        if (remember) CaptureScroll(true);
        Canvas.ForceUpdateCanvases();
        article.StopMovement();
        float height = article.content.rect.height - article.viewport.rect.height;
        var rect = entries[index].rect;
        float top = -rect.anchoredPosition.y - (1f - rect.pivot.y) * rect.rect.height - 12f;
        article.verticalNormalizedPosition = height <= 0f ? 1f : 1f - Mathf.Clamp01(top / height);
        if (remember)
        {
            AppendHistory(article.verticalNormalizedPosition);
        }
    }

    void TraverseHistory(int delta)
    {
        CaptureScroll(true);
        if (historyIndex + delta < 0 || historyIndex + delta >= History.Count) return;
        historyIndex += delta;
        article.StopMovement();
        article.verticalNormalizedPosition = History[historyIndex];
        observedPosition = article.verticalNormalizedPosition;
        UpdateHistoryButtons();
    }

    public void Search(string query)
    {
        matches = RulesDocument.Search(plain, query);
        activeMatch = -1;
        foreach (Entry entry in entries) entry.resultButton.gameObject.SetActive(false);
        foreach (int block in matches.Select(m => m.block).Distinct().OrderByDescending(i => entries[i].heading))
        {
            var button = entries[block].resultButton;
            button.gameObject.SetActive(true);
            button.transform.SetAsLastSibling();
            int first = matches.Find(m => m.block == block).start;
            int start = Mathf.Max(0, first - 35);
            string excerpt = plain[block].Substring(start, Mathf.Min(115, plain[block].Length - start));
            button.GetComponentInChildren<TMP_Text>().text = entries[block].section + "\n" + (start > 0 ? "…" : "") + excerpt;
        }
        resultsScroll.gameObject.SetActive(!string.IsNullOrWhiteSpace(query));
        resultsScroll.verticalNormalizedPosition = 1f;
        RefreshMatches();
    }

    public void StepMatch(int direction)
    {
        if (matches.Count == 0) return;
        SelectMatch(activeMatch < 0 ? (direction > 0 ? 0 : matches.Count - 1) : (activeMatch + direction + matches.Count) % matches.Count);
    }

    void SelectMatch(int index)
    {
        if (index < 0 || index >= matches.Count) return;
        activeMatch = index;
        CaptureScroll(true);
        Jump(matches[index].block, false);
        // A paragraph may be taller than the viewport. Bring the actual matching line into view.
        Entry entry = entries[matches[index].block];
        entry.text.ForceMeshUpdate();
        int character = Mathf.Min(matches[index].start, entry.text.textInfo.characterCount - 1);
        if (character >= 0)
        {
            float lineOffset = Mathf.Max(0f, entry.text.textInfo.characterInfo[0].ascender - entry.text.textInfo.characterInfo[character].ascender);
            float range = article.content.rect.height - article.viewport.rect.height;
            if (range > 0) article.verticalNormalizedPosition = Mathf.Clamp01(article.verticalNormalizedPosition - lineOffset / range);
        }
        AppendHistory(article.verticalNormalizedPosition);
        RefreshMatches();
    }

    void RefreshMatches()
    {
        matchCount.text = string.IsNullOrWhiteSpace(search.text) ? "Search the rules" : matches.Count == 0 ? "No matches" :
            (activeMatch < 0 ? "" : (activeMatch + 1) + " / ") + matches.Count + " matches";
        nextButton.interactable = previousButton.interactable = matches.Count > 0;
        for (int i = 0; i < entries.Length; i++)
        {
            TMP_Text text = entries[i].text;
            text.ForceMeshUpdate(true);
            foreach (var match in matches.Where(m => m.block == i))
            {
                bool active = activeMatch >= 0 && matches[activeMatch].block == i && matches[activeMatch].start == match.start;
                Color32 color = active ? new Color32(255, 176, 70, 255) : new Color32(255, 236, 135, 255);
                for (int c = match.start; c < match.start + match.length && c < text.textInfo.characterCount; c++)
                {
                    var character = text.textInfo.characterInfo[c];
                    if (!character.isVisible) continue;
                    var colors = text.textInfo.meshInfo[character.materialReferenceIndex].colors32;
                    for (int v = 0; v < 4; v++) colors[character.vertexIndex + v] = color;
                }
            }
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
    }

    void UpdateContents()
    {
        float top = article.content.anchoredPosition.y + 40f;
        int current = -1;
        for (int i = 0; i < entries.Length; i++)
            if (entries[i].contentsButton != null && -entries[i].rect.anchoredPosition.y - entries[i].rect.rect.height * (1f - entries[i].rect.pivot.y) <= top) current = i;
        if (current == highlightedContents) return;
        if (highlightedContents >= 0) entries[highlightedContents].contentsButton.image.color = new Color32(20, 46, 68, 255);
        highlightedContents = current;
        if (current >= 0) entries[current].contentsButton.image.color = new Color32(38, 91, 116, 255);
    }
}
