using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CounterAttack.Online
{
    /// <summary>Both managers use the same published choices and authenticated command lane.</summary>
    public sealed class OnlineBoardInput:MonoBehaviour
    {
        public OnlineGroundPassBoard board;
        public bool samplePhysicalInput=true;
        private OnlineGroundPassState state;
        private static readonly KeyCode[] Keys={KeyCode.P,KeyCode.L,KeyCode.C,KeyCode.F,KeyCode.M,KeyCode.S,KeyCode.R,KeyCode.D,KeyCode.B,KeyCode.N,KeyCode.T,KeyCode.Return,KeyCode.KeypadEnter};
        public void Display(OnlineGroundPassState publication)=>state=publication;
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
        public bool TryKey(KeyCode key)=>Submit(OnlineBoardActionPicker.Key(state,key));
        public static bool KeyboardOwnedByUI(KeyCode key)
        {
            var selected=EventSystem.current?.currentSelectedGameObject;
            return selected!=null && (selected.GetComponentInParent<InputField>()!=null
                || selected.GetComponentInParent<TMP_InputField>()!=null
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
            if(!samplePhysicalInput || board==null || !board.liveFullRoom || RoomInputOwnership.BlocksHover)return;
            if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)
                || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))return;
            foreach(var key in Keys)if(Input.GetKeyDown(key) && !KeyboardOwnedByUI(key)){TryKey(key);break;}
            if(Input.GetMouseButtonDown(0) && board.boardCamera!=null && !PointerOwnedByUI())TryRay(board.boardCamera.ScreenPointToRay(Input.mousePosition));
        }
    }
}
