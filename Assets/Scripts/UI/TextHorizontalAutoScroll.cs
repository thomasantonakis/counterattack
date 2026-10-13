using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class HorizontalTextAutoScroll : MonoBehaviour
{
    [Header("References")]
    public RectTransform panelRectTransform;   // The viewport / panel that defines visible area

    [Header("Timing")]
    public float startDelay = 2f;             // Time before scroll starts
    public float scrollSpeed = 50f;           // Pixels per second, leftwards
    public float resetDelay = 2f;             // Time to wait after scroll ends before resetting

    RectTransform textRectTransform;
    float startDelayTimer;
    float resetDelayTimer;

    Vector2 originalLocalPosition;
    bool scrollingActive;
    bool waitingToReset;

    void Awake()
    {
        textRectTransform = GetComponent<RectTransform>();
        originalLocalPosition = textRectTransform.localPosition;

        // Initialize timers
        startDelayTimer = 0f;
        resetDelayTimer = 0f;

        scrollingActive = false;
        waitingToReset = false;
    }

    void Update()
    {
        // 1. Wait before starting the scroll
        if (!scrollingActive && !waitingToReset)
        {
            startDelayTimer += Time.deltaTime;
            if (startDelayTimer >= startDelay)
            {
                // Check if scrolling is actually needed
                if (IsTextWiderThanPanel())
                {
                    scrollingActive = true;
                }
                else
                {
                    // Text fits → no scrolling, just stay visible
                    // Reset timer so it can check again if text changes later
                    startDelayTimer = 0f;
                }
            }
            return;
        }

        // 2. Scroll horizontally while active
        if (scrollingActive)
        {
            textRectTransform.localPosition += Vector3.left * scrollSpeed * Time.deltaTime;

            // Check if text is fully out of the panel
            if (IsTextFullyOutOfPanel())
            {
                scrollingActive = false;
                waitingToReset = true;
                resetDelayTimer = 0f;
            }

            return;
        }

        // 3. After scroll ends, wait resetDelay then snap back
        if (waitingToReset)
        {
            resetDelayTimer += Time.deltaTime;
            if (resetDelayTimer >= resetDelay)
            {
                textRectTransform.localPosition = originalLocalPosition;

                // Prepare for another cycle
                waitingToReset = false;
                startDelayTimer = 0f;
            }
        }
    }

    bool IsTextWiderThanPanel()
    {
        if (panelRectTransform == null)
            return false;

        float panelWidth = panelRectTransform.rect.width;

        float textWidth = 0f;
        var tmp = GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            textWidth = tmp.preferredWidth;
        }
        else
        {
            var uiText = GetComponent<Text>();
            if (uiText != null)
            {
                textWidth = uiText.preferredWidth;
            }
        }

        return textWidth > panelWidth;
    }

    bool IsTextFullyOutOfPanel()
    {
        if (panelRectTransform == null)
            return false;

        float panelWidth = panelRectTransform.rect.width;

        // Text width
        float textWidth = 0f;
        var tmp = GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.ForceMeshUpdate(); // keep preferredWidth accurate [web:95]
            textWidth = tmp.preferredWidth;
        }
        else
        {
            var uiText = GetComponent<Text>();
            if (uiText != null)
            {
                textWidth = uiText.preferredWidth;
            }
        }

        float currentCenterX = textRectTransform.localPosition.x;

        // When the text's RIGHT edge passes the panel's LEFT edge, it's fully out:
        // panelLeftEdge = -panelWidth / 2
        // textRightEdge = currentCenterX + textWidth / 2
        float panelLeftEdge = -panelWidth / 2f;
        float textRightEdge = currentCenterX + textWidth / 2f;

        return textRightEdge < panelLeftEdge;
    }
}