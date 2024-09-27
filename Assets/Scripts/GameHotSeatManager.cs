using UnityEngine;
using UnityEngine.SceneManagement;

public class GameHotSeatManager : MonoBehaviour
{
    public void CreateNewHotSeat()
    {
        SceneManager.LoadScene("Room"); // Replace with your game scene name
    }

    public void LoadHotSeat()
    {
        Debug.Log("Opening Load HotSeat Scene");
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu"); // Replace with your game scene name
    }
    
    public void ExitGameFromHotSeat()
    {
        Debug.Log("Game Quit!");
        Application.Quit(); // This will close the application
    }
}
