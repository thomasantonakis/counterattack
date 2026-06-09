using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FreeDraftTableFilterField : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private static int lastHandledTabFrame = -1;
    private static readonly Color NormalColor = new Color(0.92f, 0.94f, 0.95f, 1f);
    private static readonly Color HoverColor = new Color(1f, 1f, 1f, 1f);
    private static readonly Color FocusedColor = new Color(0.86f, 0.93f, 1f, 1f);
    private static readonly Color TextColor = new Color(0.07f, 0.08f, 0.09f, 1f);
    private static readonly Color SelectionColor = new Color(0.18f, 0.48f, 0.86f, 0.35f);

    private DraftManager draftManager;
    private TMP_InputField inputField;
    private TMP_Text inputText;
    private Image backgroundImage;
    private string columnKey;
    private string defaultValue;
    private string placeholderText;
    private bool numeric;
    private bool hovered;
    private bool focused;
    private bool suppressChange;
    private bool showingPlaceholder;
    private Coroutine pendingSelectPresetCoroutine;

    public void Configure(DraftManager manager, string filterColumnKey, bool isNumeric, string defaultValue, string placeholderText)
    {
        draftManager = manager;
        columnKey = filterColumnKey;
        numeric = isNumeric;
        this.defaultValue = defaultValue ?? string.Empty;
        this.placeholderText = placeholderText ?? string.Empty;

        backgroundImage = GetComponent<Image>();
        inputText = GetComponentInChildren<TMP_Text>();
        ConfigureInputField();
        SetValueWithoutNotify(this.defaultValue);
        ApplyVisualState();
    }

    public void SetValueWithoutNotify(string newValue)
    {
        if (inputField == null)
        {
            ConfigureInputField();
        }

        suppressChange = true;
        string nextValue = newValue ?? string.Empty;
        showingPlaceholder = false;
        if (string.IsNullOrEmpty(nextValue) && !focused && !string.IsNullOrEmpty(placeholderText))
        {
            showingPlaceholder = true;
            inputField.SetTextWithoutNotify(placeholderText);
            SetTextColor(new Color(0.34f, 0.38f, 0.4f, 1f));
        }
        else
        {
            inputField.SetTextWithoutNotify(nextValue);
            SetTextColor(TextColor);
        }
        suppressChange = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        hovered = true;
        ApplyVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        ApplyVisualState();
    }

    public void OnSelect(BaseEventData eventData)
    {
        focused = true;
        ClearPlaceholderForEditing();
        ApplyVisualState();
        inputField?.ActivateInputField();
        SelectPresetForOverwriteIfNeeded();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        focused = false;
        RestoreDefaultIfEmpty();
        ApplyVisualState();
    }

    private void Update()
    {
        if (!focused || inputField == null)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Tab) || lastHandledTabFrame == Time.frameCount)
        {
            return;
        }

        lastHandledTabFrame = Time.frameCount;
        int direction = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? -1 : 1;
        draftManager?.FocusAdjacentFreeDraftFilter(columnKey, direction);
    }

    public void FocusInput()
    {
        if (inputField == null)
        {
            ConfigureInputField();
        }

        EventSystem.current?.SetSelectedGameObject(gameObject);
        focused = true;
        ClearPlaceholderForEditing();
        inputField.ActivateInputField();
        int caretPosition = inputField.text?.Length ?? 0;
        inputField.caretPosition = caretPosition;
        inputField.stringPosition = caretPosition;
        ApplyVisualState();
        SelectPresetForOverwriteIfNeeded();
    }

    private void ConfigureInputField()
    {
        inputField = GetComponent<TMP_InputField>();
        if (inputField == null)
        {
            inputField = gameObject.AddComponent<TMP_InputField>();
        }

        if (inputText == null)
        {
            inputText = GetComponentInChildren<TMP_Text>();
        }

        if (inputText != null)
        {
            inputText.raycastTarget = false;
            SetTextColor(TextColor);
            inputField.textComponent = inputText;
            inputField.textViewport = inputText.rectTransform;
        }

        inputField.placeholder = null;
        inputField.targetGraphic = backgroundImage;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.contentType = TMP_InputField.ContentType.Standard;
        inputField.inputType = TMP_InputField.InputType.Standard;
        inputField.characterValidation = TMP_InputField.CharacterValidation.None;
        inputField.customCaretColor = true;
        inputField.caretColor = TextColor;
        inputField.selectionColor = SelectionColor;
        inputField.caretWidth = 2;
        inputField.caretBlinkRate = 0.8f;
        inputField.onFocusSelectAll = false;
        inputField.resetOnDeActivation = false;
        inputField.restoreOriginalTextOnEscape = false;
        inputField.onValueChanged.RemoveListener(HandleInputChanged);
        inputField.onValueChanged.AddListener(HandleInputChanged);

        Navigation navigation = inputField.navigation;
        navigation.mode = Navigation.Mode.None;
        inputField.navigation = navigation;
    }

    private void HandleInputChanged(string newValue)
    {
        if (suppressChange)
        {
            return;
        }

        showingPlaceholder = false;
        SetTextColor(TextColor);

        string filteredValue = numeric ? SanitizeNumericFilter(newValue) : SanitizeTextFilter(newValue);
        if (filteredValue != newValue)
        {
            int caretPosition = Mathf.Clamp(inputField.caretPosition - (newValue.Length - filteredValue.Length), 0, filteredValue.Length);
            suppressChange = true;
            inputField.SetTextWithoutNotify(filteredValue);
            inputField.caretPosition = caretPosition;
            inputField.stringPosition = caretPosition;
            suppressChange = false;
        }

        draftManager?.UpdateFreeDraftFilter(columnKey, filteredValue);
    }

    private void RestoreDefaultIfEmpty()
    {
        if (inputField == null || !string.IsNullOrEmpty(inputField.text))
        {
            return;
        }

        SetValueWithoutNotify(defaultValue);
        draftManager?.UpdateFreeDraftFilter(columnKey, defaultValue);
    }

    private void ClearPlaceholderForEditing()
    {
        if (!showingPlaceholder || inputField == null)
        {
            return;
        }

        suppressChange = true;
        showingPlaceholder = false;
        inputField.SetTextWithoutNotify(string.Empty);
        inputField.caretPosition = 0;
        inputField.stringPosition = 0;
        suppressChange = false;
        SetTextColor(TextColor);
    }

    private void SelectPresetForOverwriteIfNeeded()
    {
        if (inputField == null || string.IsNullOrEmpty(defaultValue) || inputField.text != defaultValue)
        {
            return;
        }

        if (pendingSelectPresetCoroutine != null)
        {
            StopCoroutine(pendingSelectPresetCoroutine);
        }

        pendingSelectPresetCoroutine = StartCoroutine(SelectPresetForOverwriteNextFrame());
    }

    private IEnumerator SelectPresetForOverwriteNextFrame()
    {
        yield return null;

        if (inputField != null && focused && inputField.text == defaultValue)
        {
            inputField.selectionStringAnchorPosition = 0;
            inputField.selectionStringFocusPosition = inputField.text.Length;
            inputField.caretPosition = inputField.text.Length;
            inputField.stringPosition = inputField.text.Length;
        }

        pendingSelectPresetCoroutine = null;
    }

    private void SetTextColor(Color color)
    {
        if (inputText != null)
        {
            inputText.color = color;
        }
    }

    private string SanitizeNumericFilter(string rawValue)
    {
        if (string.IsNullOrEmpty(rawValue))
        {
            return string.Empty;
        }

        char[] acceptedCharacters = new char[rawValue.Length];
        int acceptedCount = 0;
        foreach (char inputCharacter in rawValue)
        {
            if (char.IsDigit(inputCharacter) || inputCharacter == '<' || inputCharacter == '>' || inputCharacter == '=')
            {
                acceptedCharacters[acceptedCount] = inputCharacter;
                acceptedCount++;
            }
        }

        return new string(acceptedCharacters, 0, acceptedCount);
    }

    private string SanitizeTextFilter(string rawValue)
    {
        if (string.IsNullOrEmpty(rawValue))
        {
            return string.Empty;
        }

        char[] acceptedCharacters = new char[rawValue.Length];
        int acceptedCount = 0;
        foreach (char inputCharacter in rawValue)
        {
            if (!char.IsControl(inputCharacter))
            {
                acceptedCharacters[acceptedCount] = inputCharacter;
                acceptedCount++;
            }
        }

        return new string(acceptedCharacters, 0, acceptedCount);
    }

    private void ApplyVisualState()
    {
        if (backgroundImage == null)
        {
            return;
        }

        if (focused)
        {
            backgroundImage.color = FocusedColor;
        }
        else if (hovered)
        {
            backgroundImage.color = HoverColor;
        }
        else
        {
            backgroundImage.color = NormalColor;
        }
    }
}
