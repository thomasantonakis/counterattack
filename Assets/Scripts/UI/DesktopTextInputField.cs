using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Desktop navigation conventions missing from TMP's built-in key handler.</summary>
public class DesktopTextInputField : TMP_InputField
{
    private readonly Event processingEvent = new Event();
    private bool wasComposing;

    // TMP's KeyPressed is non-virtual, so dispatch here and leave editing, clipboard,
    // submission and cancellation to that handler.
    public override void OnUpdateSelected(BaseEventData eventData)
    {
        if (!isFocused) return;
        var currentInput = EventSystem.current != null && EventSystem.current.currentInputModule != null
            ? EventSystem.current.currentInputModule.input : null;
        bool composing = !string.IsNullOrEmpty(currentInput != null
            ? currentInput.compositionString : Input.compositionString);
        bool consumed = false;
        while (Event.PopEvent(processingEvent))
        {
            if (processingEvent.rawType == EventType.KeyDown)
            {
                consumed = true;
                if (wasComposing && !composing && processingEvent.character == 0
                    && processingEvent.modifiers == EventModifiers.None) continue;

                if (!composing && HandleNavigation(processingEvent,
                    SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)) continue;

                if (KeyPressed(processingEvent) == EditState.Finish)
                {
                    if (!wasCanceled) SendOnSubmit();
                    DeactivateInputField();
                    break;
                }
                ForceLabelUpdate();
                // TMP normally sets a private mesh-dirty flag here. Keep text info
                // current for a second queued key before the next canvas update.
                if (textComponent != null) textComponent.ForceMeshUpdate();
            }
            else if ((processingEvent.rawType == EventType.ValidateCommand
                || processingEvent.rawType == EventType.ExecuteCommand)
                && processingEvent.commandName == "SelectAll")
            {
                SelectAll();
                consumed = true;
            }
        }
        wasComposing = composing;
        if (consumed || composing)
        {
            ForceLabelUpdate();
            eventData.Use();
        }
    }

    /// <summary>Returns false for keys which should use normal TMP editing.</summary>
    public bool HandleNavigation(Event key, bool mac)
    {
        bool shift = key.shift;
        bool command = mac && key.command;
        bool word = mac ? key.alt || key.control : key.control;
        bool left = key.keyCode == KeyCode.LeftArrow;
        bool right = key.keyCode == KeyCode.RightArrow;
        bool home = key.keyCode == KeyCode.Home;
        bool end = key.keyCode == KeyCode.End;

        if (command && (key.keyCode == KeyCode.UpArrow || key.keyCode == KeyCode.DownArrow))
        {
            if (key.keyCode == KeyCode.UpArrow) MoveTextStart(shift);
            else MoveTextEnd(shift);
            return true;
        }

        // macOS also supports the Emacs-style Control+A/E line shortcuts.
        if (mac && key.control && !key.command && !key.alt)
        {
            home |= key.keyCode == KeyCode.A;
            end |= key.keyCode == KeyCode.E;
        }
        if (home || end || (command && (left || right)))
        {
            bool document = (home || end) && (mac ? key.command : key.control);
            if (document)
            {
                if (home) MoveTextStart(shift); else MoveTextEnd(shift);
            }
            else
            {
                // TMP's line methods consult the anchor; repeated Shift navigation
                // must instead start from the moving end of the selection.
                int anchor = stringPositionInternal;
                int caretAnchor = caretPositionInternal;
                caretPositionInternal = caretSelectPositionInternal;
                if (home || left) MoveToStartOfLine(shift, false);
                else MoveToEndOfLine(shift, false);
                if (shift)
                {
                    stringPositionInternal = anchor;
                    caretPositionInternal = caretAnchor;
                }
            }
            return true;
        }
        if (word && (left || right))
        {
            if (mac)
            {
                int position = stringSelectPositionInternal;
                if (stringPositionInternal != stringSelectPositionInternal && !shift)
                    position = left ? Mathf.Min(stringPositionInternal, position)
                        : Mathf.Max(stringPositionInternal, position);
                else if (left)
                {
                    while (position > 0 && !IsWordCharacter(text[position - 1])) position--;
                    while (position > 0 && IsWordCharacter(text[position - 1])) position--;
                }
                else
                {
                    while (position < text.Length && !IsWordCharacter(text[position])) position++;
                    while (position < text.Length && IsWordCharacter(text[position])) position++;
                }
                int anchor = stringPositionInternal;
                int caretAnchor = caretPositionInternal;
                stringPosition = position;
                if (shift)
                {
                    stringPositionInternal = anchor;
                    caretPositionInternal = caretAnchor;
                }
                return true;
            }
            // TMP uses Command for word movement on Apple, Control elsewhere.
            var normalized = new Event(key);
            normalized.modifiers = (shift ? EventModifiers.Shift : EventModifiers.None)
                | (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX
                    ? EventModifiers.Command : EventModifiers.Control);
            KeyPressed(normalized);
            return true;
        }
        return false;
    }

    private static bool IsWordCharacter(char character)
    {
        var category = char.GetUnicodeCategory(character);
        return char.IsLetterOrDigit(character) || character == '_'
            || category == System.Globalization.UnicodeCategory.NonSpacingMark
            || category == System.Globalization.UnicodeCategory.SpacingCombiningMark;
    }
}
