using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Authored onto menu controls and dropdown templates. Does not alter their layout or callbacks.</summary>
public sealed class MenuAudioFeedback : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISubmitHandler
{
    public bool buttonSelection = true;
    private UnityEngine.UI.Selectable selectable;
    private UnityEngine.UI.Button button;
    private TMP_Dropdown dropdown;
    private bool pendingSelection;

    private void OnEnable()
    {
        selectable = GetComponent<UnityEngine.UI.Selectable>();
        button = GetComponent<UnityEngine.UI.Button>();
        dropdown = GetComponent<TMP_Dropdown>();
        if (button != null) button.onClick.AddListener(OnButtonClick);
    }

    private void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(OnButtonClick);
        pendingSelection = false;
    }

    private bool Ready => selectable != null && selectable.IsActive() && selectable.IsInteractable();
    public void OnPointerEnter(PointerEventData data) { if (Ready) MenuAudioPlayer.Play(GameplayAudioPool.MenuHover); }
    public void OnPointerClick(PointerEventData data)
    {
        if (Ready && IsDropdownControl && data.button == PointerEventData.InputButton.Left)
            MenuAudioPlayer.Play(GameplayAudioPool.MenuSelect);
    }
    public void OnSubmit(BaseEventData data) { if (Ready && IsDropdownControl) MenuAudioPlayer.Play(GameplayAudioPool.MenuSelect); }
    // Option feedback is authored on the dropdown template and inherited by TMP's normal clones.
    // Observe actual input rather than onValueChanged, which also fires during programmatic refresh.
    private bool IsDropdownControl => dropdown != null || (selectable is UnityEngine.UI.Toggle && GetComponentInParent<TMP_Dropdown>() != null);
    private void OnButtonClick() { if (buttonSelection && Ready) pendingSelection = true; }
    private void LateUpdate() => FlushPendingSelection();
    public void FlushPendingSelection()
    {
        if (!pendingSelection) return;
        pendingSelection = false;
        // Accepted navigation callbacks have already emitted their stronger cue this frame.
        if (Ready && MenuAudioPlayer.Instance != null && MenuAudioPlayer.Instance.LastPlayedFrame != Time.frameCount)
            MenuAudioPlayer.Play(GameplayAudioPool.MenuSelect);
    }
}
