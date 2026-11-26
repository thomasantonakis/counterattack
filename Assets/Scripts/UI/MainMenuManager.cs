using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private const string CreateLoadRoomSceneName = "CreateLoadRoom";
    [SerializeField] private MenuConfirmation quitConfirmation;

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
        ApplicationManager.EnsureInstanceExists();
        SceneManager.LoadScene(CounterAttack.Online.OnlineModeMenu.SceneName);
    }

    public void PlayCampaignMode()
    {
        Debug.Log("Starting Campaign Mode!");
    }

    public void OpenRules()
    {
        RulesViewer.OpenRules();
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
        quitConfirmation.ShowQuit();
    }

    private static void OpenCreateLoadRoom(string gameMode)
    {
        ApplicationManager.EnsureInstanceExists();
        ApplicationManager.Instance.SetSelectedRoomGameMode(gameMode);
        Debug.Log($"Opening {gameMode} create/load room.");
        MenuAudioPlayer.Play(GameplayAudioPool.MenuAdvance);
        SceneManager.LoadScene(CreateLoadRoomSceneName);
    }
}
