using UnityEngine;
using UnityEngine.EventSystems;

public sealed class GameplayAudioHoverFeedback : MonoBehaviour, IPointerEnterHandler
{
    public GameplayAudioDirector director;
    public void OnPointerEnter(PointerEventData eventData)
    {
        var selectable = GetComponent<UnityEngine.UI.Selectable>();
        if (selectable != null && selectable.IsActive() && selectable.IsInteractable())
            director?.PlayGameCue(GameplayAudioPool.MenuHover);
    }
}
