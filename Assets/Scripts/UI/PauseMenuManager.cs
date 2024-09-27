using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    public GameObject pausePanel;  // Reference to the Pause Panel
    public Button resumeButton;
    public Button saveMatchButton;
    public Button saveMatchAsButton;
    public Button editSettings;
    public Button backToMainMenuButton;
    public Button quitButton;

    private bool isPaused = false;

    void Awake()
    {
        // Set up button listeners
        resumeButton.onClick.AddListener(ResumeGame);
        saveMatchButton.onClick.AddListener(SaveMatch);
        saveMatchAsButton.onClick.AddListener(SaveMatchAs);
        editSettings.onClick.AddListener(EditSettings);
        backToMainMenuButton.onClick.AddListener(BackToMainMenuInGame);
        quitButton.onClick.AddListener(QuitMatch);
        // Hide the pause menu on awake
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Pause panel reference is missing!");
        }
    }

    void Update()
    {
        // Toggle the pause menu when the Escape key is pressed
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        // Debug to see if the panel is being set active
        // Debug.Log("Pausing game and showing pause panel");
        // Display the pause menu
        pausePanel.SetActive(true);
        // Debug to confirm visibility
        // Debug.Log($"Pause panel active status: {pausePanel.activeSelf}");
        isPaused = true;
    }

    public void ResumeGame()
    {
        // Debug to see if the panel is being hidden
        // Debug.Log("Resuming game and hiding pause panel");
        // Hide the pause menu
        pausePanel.SetActive(false);
        // Debug to confirm visibility
        // Debug.Log($"Pause panel active status: {pausePanel.activeSelf}");
        isPaused = false;
    }

    public void SaveMatch()
    {
        // Implement your logic for saving the match
        Debug.Log("Match Saved!");
        ResumeGame();
    }

    public void SaveMatchAs()
    {
        // Implement your logic for "Save Match As" functionality
        Debug.Log("Save Match As triggered!");
        ResumeGame();
    }

    public void EditSettings()
    {
        // Implement your logic for "Save Match As" functionality
        Debug.Log("Entering Edit Settings Mode..");
    }

    public void BackToMainMenuInGame()
    {
        // Implement your logic for "Save Match As" functionality
        Debug.Log("Back To Main Menu Scene!");
        SceneManager.LoadScene("MainMenu");  // Adjust to your actual Main Menu scene name
    }

    public void QuitMatch()
    {
        // Implement your logic for quitting the match (return to main menu)
        Debug.Log("Game Quit!");
        Application.Quit(); // This will close the application
    }
}
