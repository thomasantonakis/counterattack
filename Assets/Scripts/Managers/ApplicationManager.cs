using System.Collections.Generic;
using UnityEngine;
using System.IO; // For file operations

public class ApplicationManager : MonoBehaviour
{
    public const string OnlineGameMode = "Online";
    public const string HotSeatGameMode = "Hot Seat";
    public const string SinglePlayerGameMode = "Single Player";

    public static ApplicationManager Instance { get; private set; }
    public List<Player> PlayerList { get; private set; }
    // Stores the latest save reference so adjacent scenes keep operating on the same JSON.
    public string LastSavedFileName { get; set; }
    public bool HasExplicitSaveContext { get; private set; }
    public string SelectedRoomGameMode { get; private set; } = HotSeatGameMode;
    public bool UseRegularFormationOnNextRoomLoad { get; private set; }
    public System.IDisposable BeginOnlineRoomSaveContext(string path)
    {
        if (!Path.IsPathFullyQualified(path)) throw new System.ArgumentException("An absolute online session path is required", nameof(path));
        var lease = new OnlineSaveContext(this);
        SetActiveSaveFilePath(path);
        SetSelectedRoomGameMode(OnlineGameMode);
        return lease;
    }
    private sealed class OnlineSaveContext : System.IDisposable
    {
        private ApplicationManager owner;
        private readonly string path, mode;
        private readonly bool explicitContext, intro;
        public OnlineSaveContext(ApplicationManager app)
        { owner=app;path=app.LastSavedFileName;mode=app.SelectedRoomGameMode;explicitContext=app.HasExplicitSaveContext;intro=app.playIntroOnNextRoomLoad; }
        public void Dispose()
        {
            if(owner==null)return;
            owner.LastSavedFileName=path;owner.HasExplicitSaveContext=explicitContext;
            owner.SelectedRoomGameMode=mode;owner.playIntroOnNextRoomLoad=intro;owner=null;
        }
    }
    private bool playIntroOnNextRoomLoad;
    public void PrepareDraftRoomIntro() => playIntroOnNextRoomLoad = true;
    public bool ConsumeDraftRoomIntro()
    {
        bool requested = playIntroOnNextRoomLoad;
        playIntroOnNextRoomLoad = false;
        return requested;
    }

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

        playIntroOnNextRoomLoad = false;
        LastSavedFileName = filePath;
        HasExplicitSaveContext = true;
    }

    public void ClearActiveSaveContext()
    {
        LastSavedFileName = string.Empty;
        HasExplicitSaveContext = false;
    }

    public void PrepareRoomLaunchWithRegularFormation()
    {
        UseRegularFormationOnNextRoomLoad = true;
    }

    public bool ConsumeRegularFormationRoomLaunch()
    {
        bool useRegularFormation = UseRegularFormationOnNextRoomLoad;
        UseRegularFormationOnNextRoomLoad = false;
        return useRegularFormation;
    }

    public void SetSelectedRoomGameMode(string gameMode)
    {
        SelectedRoomGameMode = string.IsNullOrWhiteSpace(gameMode)
            ? HotSeatGameMode
            : gameMode.Trim();
    }
}
