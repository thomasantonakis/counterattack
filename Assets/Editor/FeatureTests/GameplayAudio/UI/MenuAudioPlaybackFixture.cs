using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public sealed class MenuAudioPlaybackFixture
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private readonly MenuAudioPlayer previous = MenuAudioPlayer.Instance;
    private readonly bool previousActive;
    private readonly bool hadPreference = PlayerPrefs.HasKey("CounterAttack.Audio.Game.Enabled");
    private readonly bool previousPreference = GameplayAudioPreferences.Local.Enabled(GameplayAudioCategory.Game);
    private readonly float previousScale = Time.timeScale;
    private readonly bool previousListenerPause = AudioListener.pause;
    private readonly Scene previousScene = SceneManager.GetActiveScene();
    private Scene destination;
    private AudioClip ownedClip;
    private GameplayAudioLibrary ownedLibrary;
    private readonly GameObject root;
    private readonly MenuAudioPlayer player;
    private static FieldInfo Singleton => typeof(MenuAudioPlayer).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);

    public MenuAudioPlaybackFixture()
    {
        if (previous != null) { previousActive = previous.gameObject.activeSelf; previous.gameObject.SetActive(false); }
        Singleton.SetValue(null, null);
        GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Game, true);
        root = Owned("Owned silent menu audio"); root.SetActive(false);
        var source = root.AddComponent<AudioSource>(); source.playOnAwake = false; source.mute = true;
        var wiggleSource = root.AddComponent<AudioSource>(); wiggleSource.playOnAwake = false; wiggleSource.mute = true;
        player = root.AddComponent<MenuAudioPlayer>(); player.voice = source; player.wiggleVoice = wiggleSource;
        player.library = AssetDatabase.LoadAssetAtPath<GameplayAudioLibrary>(GameplayAudioEditorTools.LibraryPath);
        root.SetActive(true);
    }

    private GameObject Owned(string name) { var go = new GameObject(name); objects.Add(go); return go; }

    public IEnumerator RunPlayback(int index)
    {
        var cues = new[] { GameplayAudioPool.MenuHover, GameplayAudioPool.MenuSelect, GameplayAudioPool.MenuAdvance, GameplayAudioPool.MenuBack };
        Time.timeScale = 0; AudioListener.pause = true;
        Assert.That(player.voice.ignoreListenerPause, Is.True);
        Assert.That(player.TryPlay(cues[index]), Is.True);
        Assert.That(player.voice.isPlaying, Is.True);
        Assert.That(player.voice.clip, Is.SameAs(player.library.Get(cues[index])[0]));
        Assert.That(player.voice.volume, Is.EqualTo(GameplayAudioConfig.PoolGain(cues[index])).Within(.0001f));
        yield return null;
    }

    public IEnumerator RunIndependentDiceWiggle()
    {
        Assert.That(player.TryPlay(GameplayAudioPool.Dice), Is.True);
        AudioClip rollClip = player.voice.clip;
        yield return new WaitForSecondsRealtime(.11f);
        Assert.That(player.TryPlay(GameplayAudioPool.DiceWiggle), Is.True);
        Assert.That(player.voice.clip, Is.SameAs(rollClip), "Starting a wait wiggle must not interrupt the roll voice.");
        Assert.That(player.wiggleVoice.isPlaying, Is.True);
        Assert.That(player.wiggleVoice.clip, Is.SameAs(player.library.Get(GameplayAudioPool.DiceWiggle)[0]));
        Assert.That(player.TryPlay(GameplayAudioPool.Dice), Is.True, "The actual roll cue must play immediately when the player presses R.");
        Assert.That(player.library.Get(GameplayAudioPool.Dice), Does.Contain(player.voice.clip));
        AudioClip currentRollClip = player.voice.clip;
        MenuAudioPlayer.StopDiceWiggle();
        Assert.That(player.wiggleVoice.isPlaying, Is.False);
        Assert.That(player.voice.clip, Is.SameAs(currentRollClip), "Stopping a wait wiggle must leave the roll voice untouched.");
        yield return null;
    }

    public IEnumerator RunPriorityMute()
    {
        Assert.That(player.TryPlay(GameplayAudioPool.MenuAdvance), Is.True);
        Assert.That(player.TryPlay(GameplayAudioPool.MenuHover), Is.False);
        Assert.That(player.TryPlay(GameplayAudioPool.MenuSelect), Is.False);
        Assert.That(player.TryPlay(GameplayAudioPool.MenuBack), Is.False);
        GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Game, false);
        Assert.That(player.voice.isPlaying, Is.False); Assert.That(player.voice.volume, Is.Zero);
        GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Game, true);
        Assert.That(player.voice.isPlaying, Is.False);
        yield return new WaitForSecondsRealtime(.12f);
        Assert.That(player.TryPlay(GameplayAudioPool.MenuBack), Is.True);
    }

    public IEnumerator RunCheckbox()
    {
        var go = Owned("Owned checkbox"); go.AddComponent<RectTransform>();
        var toggle = go.AddComponent<UnityEngine.UI.Toggle>(); go.AddComponent<MenuAudioFeedback>();
        toggle.isOn = true; toggle.isOn = false;
        Assert.That(player.voice.clip, Is.Null, "Programmatic checkbox refresh must be silent.");
        var click = new PointerEventData(null) { button = PointerEventData.InputButton.Right };
        ExecuteEvents.Execute(go, click, ExecuteEvents.pointerClickHandler);
        Assert.That(player.voice.clip, Is.Null);
        click.button = PointerEventData.InputButton.Left;
        toggle.interactable = false;
        ExecuteEvents.Execute(go, click, ExecuteEvents.pointerClickHandler);
        Assert.That(toggle.isOn, Is.False); Assert.That(player.voice.clip, Is.Null);
        toggle.interactable = true;
        ExecuteEvents.Execute(go, click, ExecuteEvents.pointerClickHandler);
        Assert.That(toggle.isOn, Is.True);
        Assert.That(player.voice.isPlaying, Is.True);
        Assert.That(player.voice.clip, Is.SameAs(player.library.Get(GameplayAudioPool.MenuSelect)[0]));
        player.voice.Stop(); yield return new WaitForSecondsRealtime(.1f);
        ExecuteEvents.Execute(go, click, ExecuteEvents.pointerClickHandler);
        Assert.That(toggle.isOn, Is.False); Assert.That(player.voice.isPlaying, Is.True);
        player.voice.Stop(); yield return new WaitForSecondsRealtime(.1f);
        ExecuteEvents.Execute(go, new BaseEventData(null), ExecuteEvents.submitHandler);
        Assert.That(toggle.isOn, Is.True); Assert.That(player.voice.isPlaying, Is.True);
        player.voice.Stop(); yield return new WaitForSecondsRealtime(.1f);
        GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Game, false);
        ExecuteEvents.Execute(go, click, ExecuteEvents.pointerClickHandler);
        Assert.That(toggle.isOn, Is.False); Assert.That(player.voice.isPlaying, Is.False);
    }

    public IEnumerator RunPreMatchDice(bool rigged)
    {
        var go = Owned("Owned draft roll-off"); go.AddComponent<RectTransform>();
        var panel = go.AddComponent<DraftOrderPanelController>();
        panel.visuals = Owned("Owned roll-off visuals"); panel.visuals.transform.SetParent(go.transform);
        var body = Owned("Owned roll-off panel"); body.transform.SetParent(go.transform); panel.panel = body.AddComponent<RectTransform>();
        TMP_Text Label(string name)
        {
            var label = Owned(name); label.transform.SetParent(go.transform);
            return label.AddComponent<TextMeshProUGUI>();
        }
        panel.title = Label("Title"); panel.homeTeam = Label("Home"); panel.awayTeam = Label("Away");
        panel.homeResult = Label("Home roll"); panel.awayResult = Label("Away roll");
        panel.outcome = Label("Outcome"); panel.footer = Label("Footer");
        panel.homeDie = Owned("Home die").AddComponent<DuelDieView>();
        panel.awayDie = Owned("Away die").AddComponent<DuelDieView>();
        var rng = UnityEngine.Random.state;
        try
        {
            panel.Roll(); Assert.That(player.voice.clip, Is.Null, "Closed roll-off is not a roll request.");
            Assert.That(panel.Open(new GameSettings()), Is.True);
            var before = UnityEngine.Random.state;
            int expected = UnityEngine.Random.Range(1, 7); var after = UnityEngine.Random.state;
            UnityEngine.Random.state = before;
            var key = new KeyPressData(rigged ? KeyCode.Alpha4 : KeyCode.R, rigged, rigged, false);
            Assert.That(panel.HandleKey(key), Is.True);
            Assert.That(panel.homeDie.DisplayedRoll, Is.EqualTo(rigged ? 4 : expected));
            Assert.That(UnityEngine.Random.state, Is.EqualTo(rigged ? before : after));
            Assert.That(panel.IsAwaitingAwayRoll, Is.True);
            Assert.That(player.voice.isPlaying, Is.True);
            Assert.That(player.library.Get(GameplayAudioPool.Dice), Does.Contain(player.voice.clip));
        }
        finally { UnityEngine.Random.state = rng; }
        yield return null;
    }

    public IEnumerator RunPreMatchAutoRoll()
    {
        var go = Owned("Owned automatic draft roll-off"); go.AddComponent<RectTransform>();
        var panel = go.AddComponent<DraftOrderPanelController>();
        panel.visuals = Owned("Owned automatic roll-off visuals"); panel.visuals.transform.SetParent(go.transform);
        var body = Owned("Owned automatic roll-off panel"); body.transform.SetParent(go.transform); panel.panel = body.AddComponent<RectTransform>();
        TMP_Text Label(string name)
        {
            var label = Owned(name); label.transform.SetParent(go.transform);
            return label.AddComponent<TextMeshProUGUI>();
        }
        panel.title = Label("Title"); panel.homeTeam = Label("Home"); panel.awayTeam = Label("Away");
        panel.homeResult = Label("Home roll"); panel.awayResult = Label("Away roll");
        panel.outcome = Label("Outcome"); panel.footer = Label("Footer");
        panel.homeDie = Owned("Home die").AddComponent<DuelDieView>();
        panel.awayDie = Owned("Away die").AddComponent<DuelDieView>();
        Assert.That(panel.Open(new GameSettings()), Is.True);
        float now = Time.unscaledTime;
        panel.AdvanceRollWait(now + DecisionTimingConfig.AutomaticRollSeconds - .01f);
        Assert.That(panel.IsAwaitingHomeRoll, Is.True, "Home keeps the full five-second decision window.");
        Assert.That(player.wiggleVoice.clip, Is.SameAs(player.library.Get(GameplayAudioPool.DiceWiggle)[0]));
        panel.AdvanceRollWait(now + DecisionTimingConfig.AutomaticRollSeconds + .01f);
        Assert.That(panel.IsAwaitingAwayRoll, Is.True, "Home rolls automatically after five seconds.");
        Assert.That(player.wiggleVoice.isPlaying, Is.False, "The wait wiggle stops when its roll resolves.");
        float awayStart = Time.unscaledTime;
        panel.AdvanceRollWait(awayStart + DecisionTimingConfig.AutomaticRollSeconds - .01f);
        Assert.That(panel.IsAwaitingAwayRoll, Is.True, "Away also gets a full five-second window.");
        panel.AdvanceRollWait(awayStart + DecisionTimingConfig.AutomaticRollSeconds + .01f);
        Assert.That(panel.IsAwaitingAwayRoll, Is.False, "Away rolls automatically after five seconds.");
        yield return null;
    }

    public IEnumerator RunDropdown()
    {
        var canvas = Owned("Owned menu canvas"); canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var eventSystem = Owned("Owned event system").AddComponent<EventSystem>();
        var go = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources()); objects.Add(go);
        go.transform.SetParent(canvas.transform, false);
        foreach (var text in go.GetComponentsInChildren<TMP_Text>(true)) text.font = TMP_Settings.defaultFontAsset;
        var dropdown = go.GetComponent<TMP_Dropdown>(); dropdown.ClearOptions(); dropdown.AddOptions(new List<string> { "One", "Two" });
        var feedback = go.AddComponent<MenuAudioFeedback>();
        dropdown.template.GetComponentInChildren<UnityEngine.UI.Toggle>(true).gameObject.AddComponent<MenuAudioFeedback>();
        dropdown.value = 1; dropdown.value = 0;
        Assert.That(player.voice.clip, Is.Null, "Initial and programmatic changes must be silent.");
        yield return null; // TMP initializes its popup tween runner in Start.
        dropdown.Show(); Assert.That(dropdown.IsExpanded, Is.True);
        dropdown.value = 1; dropdown.value = 0;
        Assert.That(player.voice.clip, Is.Null, "An expanded programmatic refresh is also silent.");
        feedback.OnPointerClick(new PointerEventData(eventSystem));
        Assert.That(player.voice.isPlaying, Is.True);
        player.voice.Stop();
        yield return new WaitForSecondsRealtime(.1f);
        var option = dropdown.GetComponentsInChildren<UnityEngine.UI.Toggle>(false)
            .First(t => t.GetComponentInChildren<TMP_Text>().text == "Two");
        ExecuteEvents.Execute(option.gameObject, new PointerEventData(eventSystem), ExecuteEvents.pointerClickHandler);
        Assert.That(dropdown.value, Is.EqualTo(1));
        Assert.That(player.voice.isPlaying, Is.True);
        Assert.That(player.voice.clip, Is.SameAs(player.library.Get(GameplayAudioPool.MenuSelect)[0]));
        dropdown.Hide();
    }

    public IEnumerator RunButtonAndRejectedAction()
    {
        var go = Owned("Owned menu button"); go.AddComponent<RectTransform>();
        var button = go.AddComponent<UnityEngine.UI.Button>(); var feedback = go.AddComponent<MenuAudioFeedback>();
        button.interactable = false;
        feedback.OnPointerEnter(new PointerEventData(null));
        Assert.That(player.voice.clip, Is.Null);
        button.interactable = true; button.onClick.Invoke(); feedback.FlushPendingSelection();
        Assert.That(player.voice.clip, Is.SameAs(player.library.Get(GameplayAudioPool.MenuSelect)[0]));
        yield return new WaitForSecondsRealtime(.12f);
        player.TryPlay(GameplayAudioPool.MenuAdvance);
        button.onClick.Invoke(); feedback.FlushPendingSelection();
        Assert.That(player.voice.clip, Is.SameAs(player.library.Get(GameplayAudioPool.MenuAdvance)[0]));
        var draft = Owned("Owned rejected draft"); draft.SetActive(false);
        var manager = draft.AddComponent<DraftUIManager>();
        int frame = player.LastPlayedFrame;
        manager.OnStartGameButtonPressed();
        Assert.That(player.LastPlayedFrame, Is.EqualTo(frame), "An incomplete draft cannot emit advancement.");
    }

    public IEnumerator RunSceneLifetime()
    {
        ownedClip = AudioClip.Create("Owned silent long navigation", 48000 * 2, 1, 48000, false);
        ownedLibrary = ScriptableObject.CreateInstance<GameplayAudioLibrary>();
        ownedLibrary.pools = new[] { new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.MenuAdvance, clips = new[] { ownedClip } } };
        player.library = ownedLibrary;
        Assert.That(player.TryPlay(GameplayAudioPool.MenuAdvance), Is.True);
        destination = SceneManager.CreateScene("Owned menu transition destination"); SceneManager.SetActiveScene(destination);
        var duplicate = Owned("Owned destination authored player"); duplicate.SetActive(false);
        var other = duplicate.AddComponent<MenuAudioPlayer>(); other.voice = duplicate.AddComponent<AudioSource>(); other.voice.playOnAwake = false;
        other.library = ownedLibrary; duplicate.SetActive(true);
        yield return null;
        Assert.That(duplicate == null, Is.True);
        Assert.That(MenuAudioPlayer.Instance, Is.SameAs(player));
        Assert.That(root.scene.name, Is.EqualTo("DontDestroyOnLoad"));
        Assert.That(player.voice.isPlaying, Is.True);
        Assert.That(player.voice.clip, Is.SameAs(ownedClip));
    }

    public IEnumerator Cleanup()
    {
        foreach (var go in objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
        if (ownedLibrary != null) UnityEngine.Object.DestroyImmediate(ownedLibrary);
        if (ownedClip != null) UnityEngine.Object.DestroyImmediate(ownedClip);
        Time.timeScale = previousScale; AudioListener.pause = previousListenerPause;
        GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Game, previousPreference);
        if (!hadPreference) PlayerPrefs.DeleteKey("CounterAttack.Audio.Game.Enabled"); PlayerPrefs.Save();
        Singleton.SetValue(null, previous);
        if (previous != null) previous.gameObject.SetActive(previousActive);
        if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
        if (destination.IsValid() && destination.isLoaded)
        { var unload = SceneManager.UnloadSceneAsync(destination); while (unload != null && !unload.isDone) yield return null; }
    }
}
