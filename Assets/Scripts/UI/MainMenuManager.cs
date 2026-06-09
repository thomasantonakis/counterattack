using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private const string CreateLoadRoomSceneName = "CreateLoadRoom";

    public void PlaySinglePlayerGame()
    {
        OpenCreateLoadRoom(ApplicationManager.SinglePlayerGameMode);
    }

    public void PlayHotSeatGame()
    {
        OpenCreateLoadRoom(ApplicationManager.HotSeatGameMode);
    }

    public void PlayMultiPlayerGame()
    {
        Debug.Log("Starting Multi Player Game!");
    }

    public void PlayCampaignMode()
    {
        Debug.Log("Starting Campaign Mode!");
    }

    public void OpenRules()
    {
        Debug.Log("Rules Opened!");
    }

    public void OpenShop()
    {
        Debug.Log("Options Opened!");
    }

    public void OpenCredits()
    {
        Debug.Log("Credits Opened!");
    }

    public void ExitGameFromMainMenu()
    {
        Debug.Log("Game Quit!");
        Application.Quit(); // This will close the application
    }

    private static void OpenCreateLoadRoom(string gameMode)
    {
        ApplicationManager.EnsureInstanceExists();
        ApplicationManager.Instance.SetSelectedRoomGameMode(gameMode);
        Debug.Log($"Opening {gameMode} create/load room.");
        SceneManager.LoadScene(CreateLoadRoomSceneName);
    }
}
