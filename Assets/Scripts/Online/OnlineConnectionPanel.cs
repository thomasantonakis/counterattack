using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CounterAttack.Online
{
    public sealed class OnlineConnectionPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private TMP_Text reading;
        [SerializeField] private bool showBeforeSession;
        [SerializeField] private GameObject roomRulesButton;
        [SerializeField] private GameObject onlineRoomRulesButton;

        private OnlineFrontendSession observedSession;
        private OnlineConnectionQuality observedState;
        private double nextRefresh;
        private RectTransform rect;
        private Image background;
        private Vector2 normalAnchorMin, normalAnchorMax, normalPivot, normalPosition;
        private Vector3 normalScale;
        private Color normalColor;
        private float normalFontMin, normalFontMax;
        private TextWrappingModes normalWrapping;
        private int normalSibling;
        private bool showingDeparture;
        private RectTransform roomPauseCard;
        private Vector2 normalPausePosition;
        private Vector3 normalPauseScale;

        private void Awake()
        {
            rect = (RectTransform)transform;
            background = GetComponent<Image>();
            normalAnchorMin = rect.anchorMin;
            normalAnchorMax = rect.anchorMax;
            normalPivot = rect.pivot;
            normalPosition = rect.anchoredPosition;
            normalScale = rect.localScale;
            normalSibling = rect.GetSiblingIndex();
            normalColor = background.color;
            normalFontMin = reading.fontSizeMin;
            normalFontMax = reading.fontSizeMax;
            normalWrapping = reading.textWrappingMode;
        }

        private void OnEnable()
        {
            observedSession = null;
            nextRefresh = 0;
        }

        private void Update()
        {
            var session = OnlineFrontendSession.Current;
            bool visible = session != null
                ? !session.IsSpectator
                : showBeforeSession && ApplicationManager.Instance?.SelectedRoomGameMode == ApplicationManager.OnlineGameMode;
            float alpha = visible ? 1 : 0;
            if (panel.alpha != alpha)
                panel.alpha = alpha;
            if (roomRulesButton != null && roomRulesButton.activeSelf == visible)
                roomRulesButton.SetActive(!visible);
            if (onlineRoomRulesButton != null && onlineRoomRulesButton.activeSelf != visible)
                onlineRoomRulesButton.SetActive(visible);
            bool departed = visible && session?.PeerDeparted == true;
            if (showingDeparture != departed) SetDeparturePresentation(departed);
            if (departed) return;
            if (!visible)
            {
                observedSession = null;
                nextRefresh = 0;
                return;
            }

            var state = session != null ? session.ConnectionState : OnlineConnectionQuality.Disconnected;
            double now = Time.realtimeSinceStartupAsDouble;
            if (session == observedSession && state == observedState && now < nextRefresh)
                return;

            observedSession = session;
            observedState = state;
            nextRefresh = now + 1;

            var quality = state;
            double? milliseconds = null;
            if (state == OnlineConnectionQuality.Measuring)
            {
                var connection = session.CaptureConnection();
                quality = connection.Quality;
                milliseconds = connection.PingMilliseconds;
            }

            string status = quality switch
            {
                OnlineConnectionQuality.Good => "<color=#8CE7AD>Good</color>",
                OnlineConnectionQuality.Average => "<color=#FFD479>Average</color>",
                OnlineConnectionQuality.Poor => "<color=#FF9393>Poor</color>",
                OnlineConnectionQuality.Disconnected => "Not connected",
                _ => quality.ToString()
            };

            string ping = milliseconds.HasValue ? $"{milliseconds.Value:0} ms" : "—";
            string label = $"Connection: {status}\nPing: {ping}";
            if (reading.text != label)
                reading.text = label;
        }

        private void SetDeparturePresentation(bool departed)
        {
            showingDeparture = departed;
            var center = new Vector2(.5f, .5f);
            rect.anchorMin = departed ? center : normalAnchorMin;
            rect.anchorMax = departed ? center : normalAnchorMax;
            rect.pivot = departed ? center : normalPivot;
            rect.anchoredPosition = departed ? Vector2.zero : normalPosition;
            rect.localScale = departed ? normalScale * 3f : normalScale;
            background.color = departed ? new Color32(135, 28, 37, 255) : normalColor;
            bool inRoom = gameObject.scene.name == "Room";
            reading.fontSizeMin = departed ? (inRoom ? 12 : 18) : normalFontMin;
            reading.fontSizeMax = departed ? 24 : normalFontMax;
            reading.textWrappingMode = departed ? TextWrappingModes.NoWrap : normalWrapping;
            if (departed)
            {
                rect.SetAsLastSibling();
                var pause = GetComponentInParent<Canvas>()?.GetComponentInChildren<PauseMenuManager>(true);
                if (pause?.pausePanel != null && pause.pausePanel.transform.parent == rect.parent)
                {
                    rect.SetSiblingIndex(pause.pausePanel.transform.GetSiblingIndex());
                    var menu = pause.pausePanel.transform;
                    roomPauseCard = menu.childCount > 0 ? menu.GetChild(0) as RectTransform : null;
                    if (roomPauseCard != null)
                    {
                        normalPausePosition = roomPauseCard.anchoredPosition;
                        normalPauseScale = roomPauseCard.localScale;
                    }
                }
                string action = inRoom ? "Use the Pause menu to exit." : "Press Leave session to exit.";
                reading.text = $"Connection closed\nThe other manager left.\n{action}";
            }
            else
            {
                rect.SetSiblingIndex(normalSibling);
                if (roomPauseCard != null)
                {
                    roomPauseCard.anchoredPosition = normalPausePosition;
                    roomPauseCard.localScale = normalPauseScale;
                }
                roomPauseCard = null;
            }
            nextRefresh = 0;
        }

        private void LateUpdate()
        {
            if (!showingDeparture || rect.parent is not RectTransform parent) return;
            Vector2 center = parent.rect.center;
            if (roomPauseCard == null)
            {
                rect.position = parent.TransformPoint(center);
                return;
            }

            const float gap = 16f;
            float noticeHeight = rect.rect.height * Mathf.Abs(rect.localScale.y);
            float normalCardHeight = roomPauseCard.rect.height * Mathf.Abs(normalPauseScale.y);
            float spaceForCard = Mathf.Max(1, parent.rect.height - noticeHeight - 3 * gap);
            float cardScale = Mathf.Min(1, spaceForCard / Mathf.Max(1, normalCardHeight));
            roomPauseCard.localScale = normalPauseScale * cardScale;
            float cardHeight = normalCardHeight * cardScale;
            // Stack both panels down the center. Fit the Pause card when the viewport is short.
            rect.position = parent.TransformPoint(center + Vector2.up * (cardHeight + gap) * .5f);
            roomPauseCard.position = parent.TransformPoint(center - Vector2.up * (noticeHeight + gap) * .5f);
        }

    }
}
