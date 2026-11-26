using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Unity's Button gives keyboard selection priority over pointer hover.
// Keep focus on Back, but show its authored hover animation under the mouse.
internal sealed class MenuConfirmationBackHover : MonoBehaviour, IPointerEnterHandler,
    IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler
{
    private Button button;
    private bool pointerInside;
    private bool pointerPressed;
    private bool refreshHover;

    private void Awake() => button = GetComponent<Button>();

    public void OnPointerEnter(PointerEventData data)
    {
        pointerInside = true;
        refreshHover = true;
    }

    public void OnPointerExit(PointerEventData data)
    {
        pointerInside = false;
        refreshHover = true;
    }

    public void OnPointerDown(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left)
            pointerPressed = true;
    }

    public void OnPointerUp(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left)
        {
            pointerPressed = false;
            refreshHover = true;
        }
    }

    public void OnSelect(BaseEventData data) => refreshHover = true;

    private void OnDisable()
    {
        pointerInside = false;
        pointerPressed = false;
        refreshHover = false;
    }

    private void LateUpdate()
    {
        if (!refreshHover)
            return;

        refreshHover = false;
        if (pointerPressed || !button.IsInteractable())
            return;

        var animator = button.animator;
        if (animator == null || !animator.isActiveAndEnabled || !animator.hasBoundPlayables)
            return;

        // Selected has no animation curves in the authored controller. Sample
        // Normal explicitly so opening the panel cannot reveal a stale arrow.
        var triggers = button.animationTriggers;
        int state = Animator.StringToHash(pointerInside ? triggers.highlightedTrigger : triggers.normalTrigger);
        if (!animator.HasState(0, state))
            return;

        // Start the authored clip directly instead of waiting for a transition
        // from the button's automatic selection to finish.
        animator.ResetTrigger(triggers.normalTrigger);
        animator.ResetTrigger(triggers.highlightedTrigger);
        animator.ResetTrigger(triggers.selectedTrigger);
        animator.Play(state, 0, 0f);
    }
}
