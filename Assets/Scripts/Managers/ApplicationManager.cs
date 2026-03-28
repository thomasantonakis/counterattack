using System.Collections.Generic;
using UnityEngine;
using System.IO; // For file operations

public class ApplicationManager : MonoBehaviour
{
    public static ApplicationManager Instance { get; private set; }
    public List<Player> PlayerList { get; private set; }
    // Stores the latest save reference so adjacent scenes keep operating on the same JSON.
    public string LastSavedFileName { get; set; }
    public bool HasExplicitSaveContext { get; private set; }

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
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            Debug.Log($"Created save folder path: {folderPath}");
        }
        Debug.Log($"Save folder path: {folderPath}");
        return folderPath; // Centralized access to the folder path
    }

    public string GetLastSavedFilePath()
    {
        if (string.IsNullOrEmpty(LastSavedFileName))
        {
            return string.Empty;
        }

        return Path.IsPathRooted(LastSavedFileName)
            ? LastSavedFileName
            : Path.Combine(GetSaveFolderPath(), LastSavedFileName);
    }

    public void SetActiveSaveFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Debug.LogWarning("SetActiveSaveFilePath called with an empty path.");
            return;
        }

        LastSavedFileName = filePath;
        HasExplicitSaveContext = true;
    }

    public void ClearActiveSaveContext()
    {
        LastSavedFileName = string.Empty;
        HasExplicitSaveContext = false;
    }
}
