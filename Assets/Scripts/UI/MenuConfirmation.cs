using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MenuConfirmation : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button alternativeButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private bool closeOnEscape = true;

    private Action confirm;
    private Action alternative;
    private GameObject previousSelection;
    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        backButton.onClick.AddListener(Close);
        if (backButton.transition == Selectable.Transition.Animation)
            backButton.gameObject.AddComponent<MenuConfirmationBackHover>();
        confirmButton.onClick.AddListener(Confirm);
        if (alternativeButton != null)
            alternativeButton.onClick.AddListener(ChooseAlternative);
    }

    private void Update()
    {
        if (closeOnEscape && Input.GetKeyUp(KeyCode.Escape))
            Close();
    }

    public void Show(Action onConfirm, Action onAlternative = null)
    {
        if (gameObject.activeSelf)
            return;
        confirm = onConfirm;
        alternative = onAlternative;
        ShowError(string.Empty);
        previousSelection = EventSystem.current?.currentSelectedGameObject;
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        backButton.Select();
        MenuAudioPlayer.Play(GameplayAudioPool.MenuAdvance);
    }

    public void ShowQuit() => ShowQuit(null);

    public void ShowQuit(Action beforeQuit)
    {
        Show(() =>
        {
            Close();
            beforeQuit?.Invoke();
            Application.Quit();
        });
    }

    public bool TryClose()
    {
        if (!IsOpen) return false;
        Close();
        return true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        confirm = null;
        alternative = null;
        EventSystem.current?.SetSelectedGameObject(
            previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
        previousSelection = null;
        MenuAudioPlayer.Play(GameplayAudioPool.MenuBack);
    }

    public void ShowError(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void Confirm() => confirm?.Invoke();

    private void ChooseAlternative() => alternative?.Invoke();
}
