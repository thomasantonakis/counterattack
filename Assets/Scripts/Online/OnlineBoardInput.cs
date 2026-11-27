using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CounterAttack.Online
{
    /// <summary>Both managers use the same published choices and authenticated command lane.</summary>
    public sealed partial class OnlineBoardInput:MonoBehaviour
    {
        public OnlineGroundPassBoard board;
        public bool samplePhysicalInput=true;
        private OnlineGroundPassState state;
        private GameInputManager roomPresentation;
        private Vector2 pressPosition;
        private bool pressArmed;
        private HexCell localGoalHover;
        private HexCell remoteGoalHover;
        private static readonly KeyCode[] CameraPresets = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
            KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4 };
        private static readonly KeyCode[] Keys={KeyCode.P,KeyCode.L,KeyCode.C,KeyCode.F,KeyCode.M,KeyCode.S,KeyCode.R,KeyCode.D,KeyCode.B,KeyCode.N,KeyCode.T,KeyCode.X,
            KeyCode.Q,KeyCode.K,KeyCode.H,KeyCode.G,KeyCode.V,KeyCode.A,KeyCode.Return,KeyCode.KeypadEnter};
        public void Display(OnlineGroundPassState publication)
        {
            state = publication;
            RefreshPublishedHighlights(publication);
            PresentTargetHover(localGoalHover);
        }
        private bool Submit(string action)=>action!=null && board!=null && board.controls!=null && board.controls.TrySubmitAction(action);
        public bool TryToken(PlayerToken token)
        {
            if(token==null || board?.tokens==null || !board.tokens.Contains(token))return false;
            return Submit(OnlineBoardActionPicker.Actor(state,(token.isHomeTeam?"home:":"away:")+token.jerseyNumber));
        }
        public bool TryHex(HexCell cell)
        {
            if(cell==null || board?.cells==null || !board.cells.Contains(cell))return false;
            return Submit(OnlineBoardActionPicker.Hex(state,cell.coordinates.x,cell.coordinates.z));
        }
        public bool TryRay(Ray ray)
        {
            if(board==null)return false;
            foreach(var hit in Physics.RaycastAll(ray,1000f,~0,QueryTriggerInteraction.Ignore).OrderBy(hit=>hit.distance))
            {
                var token=hit.collider.GetComponentInParent<PlayerToken>();
                if(token!=null && board.tokens.Contains(token))return TryToken(token)||TryHex(token.currentHex);
                var hex=hit.collider.GetComponentInParent<HexCell>();
                if(hex!=null && board.cells.Contains(hex))return TryToken(hex.occupyingToken)||TryHex(hex);
            }
            return false;
        }
        public bool TryKey(KeyCode key)
        {
            if (OnlineRoomSession.IsSpectator && key != KeyCode.Space) return false;
            if (KeyboardOwnedByUI(key) && !CanRollCoinFromBoard(key)) return false;
            if (key != KeyCode.Space) return Submit(OnlineBoardActionPicker.Key(state,key));
            if (RoomInputOwnership.IntroActive || RulesViewer.IsOpen
                || !OnlineRoomSession.IsSpectator && GameInputManager.IsPresentationModalActive) return false;
            var card = RoomInspectionCard.Instance;
            if (card == null) return false;
            if (card.IsOpen)
            {
                card.Close();
                return true;
            }
            var token = roomPresentation?.InspectionHoverToken;
            if (token == null) return false;
            card.Toggle(token);
            return card.IsOpen;
        }
        public static bool KeyboardOwnedByUI(KeyCode key)
        {
            var selected=EventSystem.current?.currentSelectedGameObject;
            return selected!=null && (selected.GetComponentInParent<InputField>()!=null
                || selected.GetComponentInParent<TMP_InputField>()!=null
                || selected.GetComponentInParent<Dropdown>()!=null || selected.GetComponentInParent<TMP_Dropdown>()!=null
                || selected.GetComponentInParent<Toggle>()!=null || selected.GetComponentInParent<Slider>()!=null
                || (key==KeyCode.Return || key==KeyCode.KeypadEnter) && selected.GetComponentInParent<Selectable>()!=null);
        }
        private static bool PointerOwnedByUI()
        {
            if(EventSystem.current==null)return false;
            var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=Input.mousePosition},hits);
            return hits.Any(hit=>hit.gameObject.GetComponentInParent<Selectable>()!=null
                || GameplayInputConsumer.FindConsumer(hit.gameObject)?.ConsumesPointerOverSelf==true
                || hit.gameObject.GetComponentInParent<MatchStatsUI>()!=null);
        }
        private void Update()
        {
            if(!samplePhysicalInput || board==null || !board.liveFullRoom)return;
            if(RoomInputOwnership.CardOpen && Input.GetKeyDown(KeyCode.Space) && TryKey(KeyCode.Space)) return;
            if(RoomInputOwnership.BlocksHover)
            { pressArmed=false;PresentHover(default,false);return; }
            if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)
                || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            { pressArmed=false;PresentHover(default,false);return; }
            if (board.boardCamera != null && board.boardCamera.TryGetComponent<CameraController>(out var camera))
            {
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                foreach (var key in CameraPresets)
                    if (Input.GetKeyDown(key) && !KeyboardOwnedByUI(key))
                        camera.TryHandlePresetKey(new KeyPressData(key, shift, ctrl: false, alt: false));
            }
            bool spectator = OnlineRoomSession.IsSpectator;
            if (!spectator)
                foreach(var key in Keys)if(Input.GetKeyDown(key)){TryKey(key);break;}
            if(board.boardCamera==null)return;
            bool overUi=PointerOwnedByUI();
            var ray=board.boardCamera.ScreenPointToRay(Input.mousePosition);
            PresentHover(ray,!overUi && (spectator || !GameInputManager.IsPresentationModalActive));
            if (Input.GetKeyDown(KeyCode.Space) && TryKey(KeyCode.Space))
            { pressArmed=false;return; }
            if (spectator) return;
            if(Input.GetMouseButtonDown(0))
            { pressArmed=!overUi && !Input.GetMouseButton(1);pressPosition=Input.mousePosition; }
            float threshold=roomPresentation!=null?roomPresentation.dragThreshold:10f;
            if(overUi || Input.GetMouseButton(1) || (pressPosition-(Vector2)Input.mousePosition).sqrMagnitude>threshold*threshold)
                pressArmed=false;
            if(Input.GetMouseButtonUp(0))
            { if(pressArmed)TryRay(ray);pressArmed=false; }
        }

        public PlayerToken PresentHover(Ray ray,bool allow)
        {
            if(roomPresentation==null)
                roomPresentation=FindAnyObjectByType<GameInputManager>(FindObjectsInactive.Include);
            PlayerToken token=null;
            HexCell hex=null;
            if(allow && board!=null)
            {
                foreach(var hit in Physics.RaycastAll(ray,1000f,~0,QueryTriggerInteraction.Ignore).OrderBy(hit=>hit.distance))
                {
                    token=hit.collider.GetComponentInParent<PlayerToken>();
                    if(token!=null && board.tokens.Contains(token)){hex=token.currentHex;break;}
                    token=null;
                    hex=hit.collider.GetComponentInParent<HexCell>();
                    if(hex==null || !board.cells.Contains(hex)){hex=null;continue;}
                    token=hex.occupyingToken;
                    break;
                }
            }
            if(token!=null && token.IsMoving)token=null;
            roomPresentation?.PresentOnlineHover(token,hex);
            PresentGroundPassHover(hex);
            PresentTargetHover(hex);
            return token;
        }

        private string GoalHoverAction(HexCell hex)
        {
            if (hex == null || hex.isInGoal == 0 || hex.IsCommittedGoalTarget) return null;
            var action = OnlineBoardActionPicker.Hex(state, hex.coordinates.x, hex.coordinates.z);
            if (action != null && (action.StartsWith("shot.target:", System.StringComparison.Ordinal)
                || action.StartsWith("header.goal:", System.StringComparison.Ordinal)
                || action.StartsWith("movement.target:", System.StringComparison.Ordinal)
                || action.StartsWith("duel.target:", System.StringComparison.Ordinal))) return action;
            return state?.legalActions?.Contains("shot.confirm") == true
                && state.hexPreviews?.Any(preview => preview.x == hex.coordinates.x && preview.z == hex.coordinates.z
                    && preview.reason == "CanShootFrom") == true ? "shot.confirm" : null;
        }

        internal void CapturePointerHover(OnlineRoomPointer pointer)
        {
            if (roomPresentation?.hoveredToken is { } token && token.GetCurrentHex() != null)
                pointer.hoveredToken = OnlineRollPublication.ActorId(token);
            var action = GoalHoverAction(localGoalHover);
            if (action == null || board.controls?.CanSubmitAction(action) != true) return;
            pointer.goalX = localGoalHover.coordinates.x;
            pointer.goalZ = localGoalHover.coordinates.z;
        }

        internal void PresentPeerHover(OnlineRoomPointer pointer)
        {
            bool canShow = isActiveAndEnabled && board != null && pointer != null && pointer.icon != 2
                && !RoomInputOwnership.BlocksHover && !GameInputManager.IsPresentationModalActive;
            PlayerToken token = null;
            HexCell goal = null;
            if (canShow)
            {
                if (pointer.hoveredToken != null)
                    token = board.tokens.FirstOrDefault(candidate => candidate.GetCurrentHex() != null && !candidate.IsMoving
                        && OnlineRollPublication.ActorId(candidate) == pointer.hoveredToken);
                if (pointer.goalX is int x && pointer.goalZ is int z
                    && state?.owner == (board.localTeam == OnlineTeam.Home ? "Away" : "Home")
                    && !state.connectionHeld && !state.isSharedPaused)
                {
                    var hex = board.grid.GetHexCellAt(new Vector3Int(x, 0, z));
                    if (GoalHoverAction(hex) != null) goal = hex;
                }
            }
            if (roomPresentation == null)
                roomPresentation = FindAnyObjectByType<GameInputManager>(FindObjectsInactive.Include);
            if (roomPresentation != null) roomPresentation.PresentOnlinePeerHover(token);
            if (remoteGoalHover == goal) return;
            if (remoteGoalHover != null) remoteGoalHover.SetRemoteGoalHover(false);
            remoteGoalHover = goal;
            if (remoteGoalHover != null) remoteGoalHover.SetRemoteGoalHover(true);
        }

        private bool CanRollCoinFromBoard(KeyCode key) => key == KeyCode.R
            && board?.controls?.CanSubmitAction("coin.roll") == true;

        private void SetLocalGoalHover(HexCell hex)
        {
            if(localGoalHover==hex)return;
            if(localGoalHover!=null)localGoalHover.SetLocalGoalHover(false);
            localGoalHover=hex;
            if(localGoalHover!=null)localGoalHover.SetLocalGoalHover(true);
        }
    }
}
