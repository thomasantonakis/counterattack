using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DraftUIManager : MonoBehaviour
{
    public Button startGameButton;  // Reference to the Start Game button
    private DraftManager draftManager;  // Reference to the DraftManager

    void Start()
    {
        // Find the DraftManager script in the scene
        draftManager = FindObjectOfType<DraftManager>();

        // Initially disable the Start Game button
        startGameButton.interactable = false;
    }
    // Method to load the previous scene
    public void OnBackButtonPressed()
    {
        // Assuming you want to load a previous scene, change "PreviousSceneName" to the actual scene name
        SceneManager.LoadScene("CreateNewHSGameScene");
    }
    public void OnBackToMainButtonPressed()
    {
        // Assuming you want to load a previous scene, change "PreviousSceneName" to the actual scene name
        SceneManager.LoadScene("MainMenu");
    }

    // Method to load the game room scene
    public void OnStartGameButtonPressed()
    {
        // Assuming "GameRoomScene" is the name of the next scene
        SceneManager.LoadScene("Room");
    }

    // Method to check if the draft is complete and enable the Start Game button
    public void CheckIfDraftIsComplete()
    {
        if (draftManager.draftPool.Count == 0)  // If all cards have been dealt
        {
            startGameButton.interactable = true;  // Enable the Start Game button
        }
    }
}
