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
            yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(OnlineModeMenu.SceneName));
            var onlineMenu = Object.FindAnyObjectByType<OnlineModeMenu>();
            Assert.That(onlineMenu, Is.Not.Null);
            if (branch == "invalid-join")
            {
                onlineMenu.lobbyCode.text = "bad";
                onlineMenu.Join();
                Assert.That(OnlineFrontendSession.Current, Is.Null);
                Assert.That(onlineMenu.status.text, Does.Contain("8-digit"));
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(OnlineModeMenu.SceneName));
                yield break;
            }
            onlineMenu.Host();
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
        if(branch=="save-guard")
        {
            panel.setup.SaveGameSettingsToJson();Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("CreateNewGameScene"));
            Assert.That(OnlineFrontendSession.Current,Is.Null);Assert.That(panel.connectionStatus.text,Does.Contain("Start an online session"));
        }
        if (branch == "host-cancel" || branch == "host-retry")
        {
            string firstLobby = null;
            for (int attempt = 0; attempt < (branch == "host-retry" ? 2 : 1); attempt++)
            {
                bool previousBackground=Application.runInBackground;
                panel.setup.ApplyOnlineSetupSettings(OnlineSetupAgreementEditorTests.Valid());
                panel.hostButton.onClick.Invoke();var session=OnlineFrontendSession.Current;
                Assert.That(session,Is.Not.Null);Assert.That(session.Hosting,Is.True);Assert.That(Application.runInBackground,Is.True);
                double deadline=Time.realtimeSinceStartupAsDouble+65;
                while(session.Busy && session.LobbyCode==null && Time.realtimeSinceStartupAsDouble<deadline)yield return null;
                Assert.That(session.Error,Is.Null,session.Error);Assert.That(session.Busy,Is.True);
                Assert.That(OnlineFrontendSessionPolicy.ValidLobbyCode(session.LobbyCode),Is.True,"Native lobby creation must complete");
                Assert.That(session.Client,Is.Null,"No remote manager has authenticated yet");
                if (attempt == 0) firstLobby = session.LobbyCode;
                else Assert.That(session.LobbyCode, Is.Not.EqualTo(firstLobby), "Retry must create a fresh lobby");
                string directory=session.RunDirectory;panel.leaveButton.onClick.Invoke();
                deadline=Time.realtimeSinceStartupAsDouble+15;
                while(OnlineFrontendSession.Current!=null && Time.realtimeSinceStartupAsDouble<deadline)yield return null;
                Assert.That(OnlineFrontendSession.Current,Is.Null,"Cancel must dispose the lobby/session without waiting for a guest");
                Assert.That(Application.runInBackground,Is.EqualTo(previousBackground));
                string report=System.IO.File.ReadAllText(System.IO.Path.Combine(directory,"frontend-session-report.json"));
                var json=Newtonsoft.Json.Linq.JObject.Parse(report);
                Assert.That((string)json["status"],Is.EqualTo("closed"));Assert.That((string)json["routingPolicy"],Is.EqualTo("AllowRelays"));
                string evidence=SessionState.GetString("CounterAttack.FrameworkResultDirectory","");
                if(!string.IsNullOrEmpty(evidence))System.IO.File.WriteAllText(System.IO.Path.Combine(evidence,"frontend-session-report.json"),report);
                Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("CreateNewGameScene"));
                Assert.That(panel.setup.createGameButton.interactable,Is.False);
            }
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
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(OnlineModeMenu.SceneName));
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
