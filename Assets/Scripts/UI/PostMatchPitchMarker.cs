using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Hover previews the recorded footprint; click pins/unpins it.</summary>
public sealed class PostMatchPitchMarker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public TMPro.TMP_Text number;
    public UnityEngine.UI.Image disc;
    private PostMatchPitchView owner;
    private PostMatchPitchData.Player player;
    public void Bind(PostMatchPitchView view, PostMatchPitchData.Player data) { owner = view; player = data; }
    public void OnPointerEnter(PointerEventData eventData) => owner?.Preview(player);
    public void OnPointerExit(PointerEventData eventData) => owner?.EndPreview();
    public void OnPointerClick(PointerEventData eventData)
    { if (eventData.button == PointerEventData.InputButton.Left) owner?.Pin(player); }
}
