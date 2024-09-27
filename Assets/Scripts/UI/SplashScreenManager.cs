using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;  // Import TextMeshPro namespace
using UnityEngine.UI;  // For accessing UI elements

public class SplashScreenManager : MonoBehaviour
{
    public Image backgroundImage;  // Background image
    public TextMeshProUGUI pressAnyKeyText;  // Press any key text
    public float fadeSpeed = 1.5f;  // Speed of the fade effect
    // private bool isFadingIn = true;

    void Start()
    {
        // Start with the "Press any key" text transparent
        pressAnyKeyText.alpha = 0f;
        StartCoroutine(FadeText());
    }

    void Update()
    {
        // Check if any key is pressed
        if (Input.anyKeyDown)
        {
            // Trigger scene transition (replace "NextSceneName" with the actual scene name)
            SceneManager.LoadScene("MainMenu");
        }
    }

    // Coroutine to fade the text in and out
    IEnumerator FadeText()
    {
        while (true)
        {
            // Fade In
            while (pressAnyKeyText.alpha < 1f)
            {
                pressAnyKeyText.alpha += Time.deltaTime * fadeSpeed;
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);  // Wait for a moment at full opacity

            // Fade Out
            while (pressAnyKeyText.alpha > 0f)
            {
                pressAnyKeyText.alpha -= Time.deltaTime * fadeSpeed;
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);  // Wait before fading in again
        }
    }
}
