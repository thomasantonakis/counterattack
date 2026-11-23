using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CounterAttack.Online
{
    public sealed class OnlineRoomPointers : MonoBehaviour
    {
        public OnlineGroundPassBoard board;
        public TMP_Text localMarker, remoteMarker;
        public RawImage localIcon, remoteIcon;
        public Texture2D hoverIcon, grabIcon, cameraDragIcon;
        private RectTransform overlay;
        private Rect pitch;
        private OnlineRoomPointer remote;
        private long remoteSequence;
        private double remoteReceivedAt;
        private bool held, ownsCursor, previousCursorVisible;

        private void Awake()
        {
            overlay = (RectTransform)localMarker.transform.parent;
            var cells = board.cells.Where(cell => !cell.isOutOfBounds).Select(cell => cell.GetHexCenter()).ToArray();
            pitch = Rect.MinMaxRect(cells.Min(point => point.x), cells.Min(point => point.z),
                cells.Max(point => point.x), cells.Max(point => point.z));
        }

        public OnlineRoomPointer CaptureLocal()
        {
            if (held || !Application.isFocused || GameInputManager.IsCameraPointerOverUi)
                return new OnlineRoomPointer();
            var pointer = Project(board.boardCamera, pitch, Input.mousePosition);
            pointer.icon = Input.GetMouseButton(1) ? 2 : Input.GetMouseButton(0) ? 1 : 0;
            return pointer;
        }

        public static OnlineRoomPointer Project(Camera camera, Rect bounds, Vector2 screen)
        {
            if (camera == null || !camera.pixelRect.Contains(screen)) return new OnlineRoomPointer();
            var ray = camera.ScreenPointToRay(screen);
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance)) return new OnlineRoomPointer();
            var world = ray.GetPoint(distance);
            return bounds.Contains(new Vector2(world.x, world.z))
                ? new OnlineRoomPointer { visible = true, x = world.x, z = world.z }
                : new OnlineRoomPointer();
        }

        public void ShowRemote(OnlineRoomPointer pointer, long sequence)
        {
            if (pointer == null || !pointer.Valid() || sequence <= remoteSequence) return;
            remoteSequence = sequence;
            remote = pointer;
            remoteReceivedAt = Time.unscaledTimeAsDouble;
        }

        public void SetHold(bool blocked)
        {
            held = blocked;
            if (blocked) remote = null;
        }

        private void LateUpdate()
        {
            var settings = board.match.gameData?.gameSettings;
            bool home = board.localTeam == OnlineTeam.Home;
            var here = CaptureLocal();
            bool hideNative = Draw(localMarker, localIcon, here, home ? settings?.homeKit : settings?.awayKit);
            if (hideNative && !ownsCursor)
            {
                previousCursorVisible = Cursor.visible;
                ownsCursor = true;
                Cursor.visible = false;
            }
            if (!hideNative) RestoreCursor();
            var other = held || Time.unscaledTimeAsDouble - remoteReceivedAt > 0.6 ? null : remote;
            Draw(remoteMarker, remoteIcon, other, home ? settings?.awayKit : settings?.homeKit);
        }

        private bool Draw(TMP_Text marker, RawImage icon, OnlineRoomPointer pointer, string kit)
        {
            if (pointer?.visible != true || !pitch.Contains(new Vector2(pointer.x, pointer.z)))
            {
                marker.gameObject.SetActive(false);
                return false;
            }
            var screen = board.boardCamera.WorldToScreenPoint(new Vector3(pointer.x, 0f, pointer.z));
            Vector2 local = default;
            bool visible = screen.z > 0 && board.boardCamera.pixelRect.Contains(screen)
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screen, null, out local);
            marker.gameObject.SetActive(visible);
            if (visible)
            {
                marker.color = TokenKitCatalog.ResolveInstructionPalette(kit, Color.white, Color.black).Primary;
                marker.rectTransform.anchoredPosition = local;
                if (icon != null)
                {
                    icon.color = marker.color;
                    icon.texture = pointer.icon == 2 ? cameraDragIcon : pointer.icon == 1 ? grabIcon : hoverIcon;
                }
            }
            return visible;
        }

        private void OnDisable() => RestoreCursor();

        private void RestoreCursor()
        {
            if (!ownsCursor) return;
            Cursor.visible = previousCursorVisible;
            ownsCursor = false;
        }
    }
}
