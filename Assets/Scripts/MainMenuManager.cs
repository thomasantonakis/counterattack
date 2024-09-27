using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void PlaySinglePlayerGame()
    {
        Debug.Log("Starting Single Player Game!");
    }

    public void PlayHotSeatGame()
    {
        SceneManager.LoadScene("Room"); // Replace with your game scene name
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

    public void ExitGame()
    {
        Debug.Log("Game Quit!");
        Application.Quit(); // This will close the application
    }
}
