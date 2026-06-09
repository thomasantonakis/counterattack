using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndGamePanelManager : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private static readonly Color OverlayColor = new(0f, 0f, 0f, 0.55f);
    private static readonly Color PanelColor = new(0.05f, 0.08f, 0.14f, 0.96f);
    private static readonly Color ButtonColor = new(0.92f, 0.92f, 0.92f, 1f);
    private static readonly Color ButtonTextColor = new(0.2f, 0.2f, 0.2f, 1f);
    private static readonly Color TitleColor = new(0.96f, 0.91f, 0.72f, 1f);
    private const float PanelWidth = 860f;
    private const float PanelHeight = 720f;
    private const float RecapHorizontalPadding = 34f;

    private GameObject root;
    private TMP_Text titleText;
    private TMP_Text recapText;
    private ScrollRect recapScroll;
    private Button mainMenuButton;
    private string titleOverride;
    private string recapOverride;

    public static void ShowMatchEndedPanel()
    {
        EndGamePanelManager manager = ResolveOrCreate();
        manager.titleOverride = null;
        manager.recapOverride = null;
        manager.ShowPanel();
    }

    public static void ShowPenaltyShootoutPendingPanel()
    {
        EndGamePanelManager manager = ResolveOrCreate();
        manager.titleOverride = "Penalties";
        manager.recapOverride = "The match is tied after extra time. Penalty shootout flow is pending.";
        manager.ShowPanel();
    }

    public static void EnsureScenePanel()
    {
        EndGamePanelManager manager = ResolveOrCreate();
        manager.EnsurePanel();
    }

    private static EndGamePanelManager ResolveOrCreate()
    {
        EndGamePanelManager existing = FindObjectsByType<EndGamePanelManager>(FindObjectsInactive.Include)
            .FirstOrDefault();
        if (existing != null)
        {
            existing.ConfigureHostRect();
            existing.EnsurePanel();
            return existing;
        }

        Canvas canvas = ResolveCanvas();
        GameObject host = new("EndGamePanelManager", typeof(RectTransform), typeof(EndGamePanelManager));
        host.transform.SetParent(canvas.transform, false);
        EndGamePanelManager manager = host.GetComponent<EndGamePanelManager>();
        manager.ConfigureHostRect();
        manager.EnsurePanel();
        Debug.Log("[EndGamePanel] Runtime end-game panel created under Canvas.");
        return manager;
    }

    private static Canvas ResolveCanvas()
    {
        Canvas canvas = FindObjectsByType<Canvas>(FindObjectsInactive.Include)
            .FirstOrDefault(candidate => candidate.isRootCanvas);
        if (canvas != null)
        {
            return canvas;
        }

        GameObject canvasObject = new("RuntimeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private void ShowPanel()
    {
        EnsurePanel();
        if (titleText != null)
        {
            titleText.text = string.IsNullOrWhiteSpace(titleOverride) ? "Full Time" : titleOverride;
        }

        recapText.text = string.IsNullOrWhiteSpace(recapOverride) ? BuildRecapText() : recapOverride;
        root.SetActive(true);
        Canvas.ForceUpdateCanvases();
        Debug.Log("[EndGamePanel] Match recap panel shown.");
        if (recapScroll != null)
        {
            recapScroll.verticalNormalizedPosition = 1f;
        }
    }

    private void EnsurePanel()
    {
        if (root != null)
        {
            if (IsPanelLayoutCurrent())
            {
                return;
            }

            Destroy(root);
            root = null;
            recapText = null;
            titleText = null;
            recapScroll = null;
            mainMenuButton = null;
        }

        ConfigureHostRect();
        Canvas canvas = GetComponentInParent<Canvas>() ?? ResolveCanvas();
        RectTransform canvasRect = canvas.transform as RectTransform;

        RectTransform parentRect = transform as RectTransform;
        root = CreateRect("EndGamePanel", parentRect != null ? parentRect : canvasRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        Image overlayImage = root.AddComponent<Image>();
        overlayImage.color = OverlayColor;
        overlayImage.raycastTarget = true;

        GameObject panel = CreateRect("RecapPanel", rootRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = PanelColor;
        panelImage.raycastTarget = true;

        titleText = CreateText("Title", panelRect, "Full Time", 34f, TitleColor, TextAlignmentOptions.Center);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -24f);
        titleRect.sizeDelta = new Vector2(-64f, 54f);

        GameObject scrollRoot = CreateRect("RecapScroll", panelRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));
        RectTransform scrollRectTransform = scrollRoot.GetComponent<RectTransform>();
        scrollRectTransform.offsetMin = new Vector2(RecapHorizontalPadding, 96f);
        scrollRectTransform.offsetMax = new Vector2(-RecapHorizontalPadding, -90f);
        recapScroll = scrollRoot.AddComponent<ScrollRect>();
        recapScroll.horizontal = false;
        recapScroll.vertical = true;
        recapScroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = CreateRect("Viewport", scrollRectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();

        GameObject content = CreateRect("Content", viewportRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 720f);
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        recapText = content.AddComponent<TextMeshProUGUI>();
        recapText.raycastTarget = false;
        recapText.richText = true;
        recapText.color = Color.white;
        recapText.fontSize = 16f;
        recapText.enableAutoSizing = true;
        recapText.fontSizeMin = 12f;
        recapText.fontSizeMax = 16f;
        recapText.alignment = TextAlignmentOptions.Top;
        recapText.textWrappingMode = TextWrappingModes.NoWrap;
        recapText.overflowMode = TextOverflowModes.Overflow;
        recapText.characterSpacing = 0f;
        recapText.lineSpacing = -2f;
        recapScroll.viewport = viewportRect;
        recapScroll.content = contentRect;

        mainMenuButton = CreateButton(panelRect);
        root.SetActive(false);
    }

    private bool IsPanelLayoutCurrent()
    {
        RectTransform panelRect = root != null
            ? root.transform.Find("RecapPanel") as RectTransform
            : null;
        return recapText != null
            && recapText.GetComponentInParent<RectMask2D>() != null
            && panelRect != null
            && Mathf.Approximately(panelRect.rect.width, PanelWidth)
            && Mathf.Approximately(panelRect.rect.height, PanelHeight)
            && Mathf.Approximately(recapText.fontSizeMax, 16f);
    }

    private void ConfigureHostRect()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private string BuildRecapText()
    {
        MatchStatsUI statsUI = FindObjectsByType<MatchStatsUI>(FindObjectsInactive.Include)
            .FirstOrDefault();
        if (statsUI != null)
        {
            float resolvedTextWidth = recapText != null ? recapText.rectTransform.rect.width : 0f;
            float textWidth = resolvedTextWidth > 400f
                ? resolvedTextWidth
                : PanelWidth - (RecapHorizontalPadding * 2f);
            return statsUI.BuildScoreAndStatsRecapText(textWidth);
        }

        MatchManager matchManager = MatchManager.Instance;
        if (matchManager == null || matchManager.gameData == null)
        {
            return "Match data not available";
        }

        string homeTeamName = matchManager.gameData.gameSettings.homeTeamName;
        string awayTeamName = matchManager.gameData.gameSettings.awayTeamName;
        int homeGoals = matchManager.gameData.stats.homeTeamStats.totalGoals;
        int awayGoals = matchManager.gameData.stats.awayTeamStats.totalGoals;
        return $"{homeTeamName} {homeGoals} - {awayGoals} {awayTeamName}";
    }

    private static GameObject CreateRect(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = Vector2.zero;
        return gameObject;
    }

    private static TMP_Text CreateText(string name, RectTransform parent, string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        TMP_Text tmpText = textObject.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.color = color;
        tmpText.fontSize = fontSize;
        tmpText.alignment = alignment;
        tmpText.raycastTarget = false;
        tmpText.characterSpacing = 0f;
        return tmpText;
    }

    private Button CreateButton(RectTransform panelRect)
    {
        GameObject buttonObject = CreateRect("BackToMainMenuButton", panelRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchoredPosition = new Vector2(0f, 26f);
        buttonRect.sizeDelta = new Vector2(300f, 54f);
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = ButtonColor;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(ReturnToMainMenu);

        TMP_Text buttonText = CreateText("Text", buttonRect, "Back To Main Menu", 23f, ButtonTextColor, TextAlignmentOptions.Center);
        RectTransform textRect = buttonText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        buttonText.fontStyle = FontStyles.SmallCaps;
        return button;
    }

    private void ReturnToMainMenu()
    {
        MatchManager.Instance?.SetPauseMenuOpen(false);
        SceneManager.LoadScene(MainMenuSceneName);
    }
}
