using System.Collections.Generic;
using UnityEngine;
using System.IO; // For file operations

public class ApplicationManager : MonoBehaviour
{
    public static ApplicationManager Instance { get; private set; }
    
    public List<Player> PlayerList { get; private set; }
    public string LastSavedFileName { get; set; } // New field for storing the file name

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Prevent this object from being destroyed on scene load
    }

    // Failsafe to ensure ApplicationManager exists
    public static void EnsureInstanceExists()
    {
        if (Instance == null)
        {
            GameObject appManagerObject = new GameObject("ApplicationManager");
            Instance = appManagerObject.AddComponent<ApplicationManager>();
        }
    }

    public void SetPlayerList(List<Player> players)
    {
        PlayerList = players;
    }

    // Provide the folder path for saving/loading files
    public string GetSaveFolderPath()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "SavedGames");
        Debug.Log($"Save folder path: {folderPath}");
        return folderPath; // Centralized access to the folder path
    }
}