using System.Collections;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class OnlineFrontendEntrySceneFixture
{
    private ApplicationManager app;
    private bool createdApp;
    private string previousMode,previousReturn,previousSettings;
    private bool hadReturn,hadSettings;
    private bool initialized;
    private const string ReturnKey="CreateNewGameReturnSource",SettingsKey="currentGameSettings";
    public IEnumerator Run(string branch)
    {
        Assert.That(OnlineFrontendSession.Current,Is.Null,"Do not replace a live session");
        app=ApplicationManager.Instance;createdApp=app==null;
        if(createdApp){ApplicationManager.EnsureInstanceExists();app=ApplicationManager.Instance;}
        previousMode=app.SelectedRoomGameMode;
        hadReturn=PlayerPrefs.HasKey(ReturnKey);previousReturn=PlayerPrefs.GetString(ReturnKey);
        hadSettings=PlayerPrefs.HasKey(SettingsKey);previousSettings=PlayerPrefs.GetString(SettingsKey);initialized=true;
        PlayerPrefs.SetString(ReturnKey,"Draft");PlayerPrefs.SetString(SettingsKey,"test-sentinel-unmodified");
        string previousSave=app.LastSavedFileName;bool previousExplicit=app.HasExplicitSaveContext;
        if(branch=="offline")
        {
            PlayerPrefs.DeleteKey(ReturnKey);app.SetSelectedRoomGameMode(ApplicationManager.HotSeatGameMode);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/CreateNewGameScene.unity",new LoadSceneParameters(LoadSceneMode.Single));
        }
        else
        {
            var menu=new GameObject("Multiplayer entry under test").AddComponent<MainMenuManager>();
            menu.PlayMultiPlayerGame();
        }
        yield return null;yield return null;
        var panel=Object.FindAnyObjectByType<OnlineSetupNetworkPanel>();Assert.That(panel,Is.Not.Null);
        Assert.That(panel.panel.activeSelf,Is.EqualTo(branch!="offline"));
        Assert.That(app.LastSavedFileName,Is.EqualTo(previousSave));Assert.That(app.HasExplicitSaveContext,Is.EqualTo(previousExplicit));
        if(branch=="offline")
        {
            Assert.That(panel.setup.createGameButton.interactable,Is.False,"Blank offline team names must still be rejected");
            panel.setup.ApplyOnlineSetupSettings(OnlineSetupAgreementEditorTests.Valid());
            yield return null;
            Assert.That(panel.setup.createGameButton.interactable,Is.True,"Valid offline selections must remain available without hosting");
            Assert.That(OnlineFrontendSession.Current,Is.Null);yield break;
        }
        Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("CreateNewGameScene"));
        Assert.That(app.SelectedRoomGameMode,Is.EqualTo(ApplicationManager.OnlineGameMode));
        Assert.That(panel.setup.createGameButton.interactable,Is.False);
        Assert.That(PlayerPrefs.GetString(SettingsKey),Is.EqualTo("test-sentinel-unmodified"));
        Assert.That(PlayerPrefs.GetString(ReturnKey),Is.EqualTo("Draft"),"Online entry must not restore or consume an offline draft");
        if(branch=="invalid-join")
        {
            panel.lobbyCode.text="bad";panel.joinButton.onClick.Invoke();
            Assert.That(OnlineFrontendSession.Current,Is.Null);Assert.That(panel.connectionStatus.text,Does.Contain("32-character"));
        }
        if(branch=="save-guard")
        {
            panel.setup.SaveGameSettingsToJson();Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("CreateNewGameScene"));
            Assert.That(OnlineFrontendSession.Current,Is.Null);Assert.That(panel.connectionStatus.text,Does.Contain("Host or join"));
        }
        if(branch=="persistent-lifetime")
        {
            var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(OnlineFrontendEntryAuthoring.PrefabPath));
            var session=root.GetComponent<OnlineFrontendSession>();Assert.That(OnlineFrontendSession.Current,Is.SameAs(session));
            Assert.That(session.Busy,Is.False);Assert.That(session.Client,Is.Null);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));
            Assert.That(OnlineFrontendSession.Current,Is.SameAs(session));session.Close();yield return null;
            Assert.That(OnlineFrontendSession.Current,Is.Null);
        }
        if(branch=="back")
        {
            panel.setup.BackToCreateLoadRoomMenu();yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MainMenu"));
        }
        Assert.That(app.LastSavedFileName,Is.EqualTo(previousSave));Assert.That(app.HasExplicitSaveContext,Is.EqualTo(previousExplicit));
    }
    public IEnumerator Cleanup()
    {
        if(!initialized)yield break;
        if(OnlineFrontendSession.Current!=null){OnlineFrontendSession.Current.Close();yield return null;}
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));
        if(hadReturn)PlayerPrefs.SetString(ReturnKey,previousReturn);else PlayerPrefs.DeleteKey(ReturnKey);
        if(hadSettings)PlayerPrefs.SetString(SettingsKey,previousSettings);else PlayerPrefs.DeleteKey(SettingsKey);
        if(app!=null){app.SetSelectedRoomGameMode(previousMode);if(createdApp)Object.Destroy(app.gameObject);}
        yield return null;
    }
}
