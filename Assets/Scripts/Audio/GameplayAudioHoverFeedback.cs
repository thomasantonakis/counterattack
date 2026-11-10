using UnityEngine;
using UnityEngine.EventSystems;

public sealed class GameplayAudioHoverFeedback : MonoBehaviour, IPointerEnterHandler
{
    public GameplayAudioDirector director;
    public void OnPointerEnter(PointerEventData eventData)
    {
        var selectable = GetComponent<UnityEngine.UI.Selectable>();
        if (selectable != null && selectable.IsActive() && selectable.IsInteractable())
        {
            if (MenuAudioPlayer.Instance != null) MenuAudioPlayer.Play(GameplayAudioPool.MenuHover);
            else director?.PlayGameCue(GameplayAudioPool.MenuHover);
        }
    }
}
