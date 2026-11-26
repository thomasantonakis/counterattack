using TMPro;
using UnityEngine;

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

        private void OnEnable()
        {
            observedSession = null;
            nextRefresh = 0;
            Update();
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
    }
}
