using System;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CounterAttack.Online
{
    public sealed class OnlineRoomRollPanels : MonoBehaviour
    {
        internal const string AcknowledgePrefix = "roll.dismiss:";
        public OnlineGroundPassBoard board;
        public DuelResolutionPanelController duel;
        public HeaderChallengePanelController header;
        public StoppageActionsPanelController stoppage;
        public CanvasGroup duelOverlay, headerOverlay, stoppageOverlay;

        private sealed class Panel
        {
            internal readonly GameObject root;
            internal readonly CanvasGroup overlay;
            internal readonly Transform[] nodes;
            internal readonly TMP_Text[] labels;
            internal readonly Image[] images;
            internal readonly DuelDieView[] dice;
            internal readonly Func<bool> open, complete;
            internal readonly Action close, fit;
            internal readonly RectTransform attackColumn, defenseColumn;

            internal Panel(GameObject root, CanvasGroup overlay, Func<bool> open, Func<bool> complete,
                Action close, Action fit, RectTransform attackColumn = null, RectTransform defenseColumn = null)
            {
                this.root = root;
                this.overlay = overlay;
                this.open = open;
                this.complete = complete;
                this.close = close;
                this.fit = fit;
                this.attackColumn = attackColumn;
                this.defenseColumn = defenseColumn;
                nodes = root.GetComponentsInChildren<Transform>(true);
                labels = root.GetComponentsInChildren<TMP_Text>(true);
                images = root.GetComponentsInChildren<Image>(true);
                dice = root.GetComponentsInChildren<DuelDieView>(true);
            }

            internal void SetLocalVisibility(bool visible)
            {
                overlay.alpha = visible ? 1 : 0;
                overlay.blocksRaycasts = visible;
            }

            internal OnlineRollPanelFrame Capture(int index, long result) => new()
            {
                panel = index, resultId = result,
                active = nodes.Select(node => node != root.transform && node.gameObject.activeSelf).ToArray(),
                labels = labels.Select(label =>
                {
                    string text = label.text ?? string.Empty;
                    return text.Length > 512 ? text.Substring(0, 512) : text;
                }).ToArray(),
                labelColors = labels.Select(label => (Color32)label.color).ToArray(),
                imageColors = images.Select(image => (Color32)image.color).ToArray(),
                dice = dice.Select(die => die.ShowsJackpot ? -1 : die.DisplayedRoll).ToArray(),
                attackColumnX = attackColumn == null ? -1 : attackColumn.anchorMin.x
            };

            internal void Display(OnlineRollPanelFrame frame)
            {
                if (nodes.Length != frame.active.Length || labels.Length != frame.labels.Length
                    || images.Length != frame.imageColors.Length || dice.Length != frame.dice.Length)
                    return;
                root.SetActive(true);
                for (int i = 0; i < nodes.Length; i++)
                    if (nodes[i] != root.transform) nodes[i].gameObject.SetActive(frame.active[i]);
                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i].text = frame.labels[i];
                    labels[i].color = frame.labelColors[i];
                }
                for (int i = 0; i < images.Length; i++) images[i].color = frame.imageColors[i];
                for (int i = 0; i < dice.Length; i++)
                {
                    int value = frame.dice[i];
                    if (value == 0) dice[i].Clear();
                    else dice[i].Resolve(value == -1 ? 6 : value, value == -1);
                }
                if (attackColumn != null && frame.attackColumnX >= 0)
                {
                    SetColumn(attackColumn, frame.attackColumnX);
                    SetColumn(defenseColumn, 0.5f - frame.attackColumnX);
                }
                fit();
            }

            private static void SetColumn(RectTransform column, float left)
            {
                column.anchorMin = new Vector2(left, column.anchorMin.y);
                column.anchorMax = new Vector2(left + 0.5f, column.anchorMax.y);
            }
        }

        private Panel[] panels;
        private OnlineRollPanelFrame shown;
        private int resultPanel = -1;
        private long resultSequence = -1, activeResult, resultCounter, hiddenResult;
        private bool homeAcknowledged, awayAcknowledged, sharedPauseVisible;

        private void Awake()
        {
            panels = new[]
            {
                new Panel(duel.visuals, duelOverlay, () => duel.IsOpen, () => duel.IsComplete,
                    () => duel.Dismiss(), duel.FitAndClamp),
                new Panel(header.visuals, headerOverlay, () => header.IsOpen, () => header.IsOnlineResultReady,
                    header.Close, header.FitAndClamp,
                    (RectTransform)header.attackTeam.transform.parent, (RectTransform)header.defenseTeam.transform.parent),
                new Panel(stoppage.visuals, stoppageOverlay, () => stoppage.IsOpen, () => stoppage.IsOnlineResultReady,
                    () => stoppage.Dismiss(), stoppage.FitAndClamp)
            };
            board.controls.RollEscapeHandler = HandleEscape;
        }

        public OnlineRollPanelFrame Capture(long rollSequence)
        {
            if (panels == null) return null;
            int index = Array.FindIndex(panels, panel => panel.open());
            if (index < 0 || !panels[index].complete())
            {
                activeResult = 0;
                resultPanel = -1;
                return index < 0 ? null : panels[index].Capture(index, 0);
            }

            if (resultPanel != index || resultSequence != rollSequence)
            {
                activeResult = ++resultCounter;
                resultPanel = index;
                resultSequence = rollSequence;
                homeAcknowledged = awayAcknowledged = false;
            }
            return panels[index].Capture(index, activeResult);
        }

        internal bool Acknowledge(OnlineTeam team, string action)
        {
            if (activeResult <= 0 || resultPanel < 0 || !panels[resultPanel].complete()
                || action != AcknowledgePrefix + activeResult.ToString(CultureInfo.InvariantCulture)) return false;
            if (team == OnlineTeam.Home) homeAcknowledged = true;
            else awayAcknowledged = true;
            if (homeAcknowledged && awayAcknowledged)
            {
                panels[resultPanel].close();
                activeResult = 0;
                resultPanel = -1;
            }
            return true;
        }

        public void Apply(OnlineRollPanelFrame frame, bool sharedPause)
        {
            if (panels == null) return;
            shown = frame;
            sharedPauseVisible = sharedPause;
            for (int index = 0; index < panels.Length; index++)
            {
                var panel = panels[index];
                bool active = frame?.panel == index;
                if (OnlineRoomSession.IsGuest)
                {
                    if (active) panel.Display(frame);
                    else panel.root.SetActive(false);
                }
                panel.SetLocalVisibility(!active || frame.resultId == 0 || frame.resultId != hiddenResult);
            }
        }

        private bool HandleEscape()
        {
            if (shown == null || sharedPauseVisible) return false;
            long resultId = shown.resultId;
            int panel = shown.panel;
            if (resultId == 0 || resultId == hiddenResult) return true;
            if (!board.controls.AcknowledgeRoll(resultId)) return true;
            hiddenResult = resultId;
            panels[panel].SetLocalVisibility(false);
            return true;
        }
    }
}
