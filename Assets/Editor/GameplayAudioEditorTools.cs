using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Explicit edit-mode authoring. Never runs automatically on reload or Play.</summary>
public static class GameplayAudioEditorTools
{
    public const string LibraryPath = "Assets/Audio/GameplayAudioLibrary.asset";

    public static string AuthorLoadedScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before authoring audio.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        bool room = scene.path == "Assets/Scenes/Room.unity";
        if (!room && scene.path != "Assets/Scenes/MainMenu.unity") throw new InvalidOperationException("Only Room or MainMenu is supported.");
        if (UnityEngine.Object.FindAnyObjectByType<GameplayAudioDirector>() != null) throw new InvalidOperationException("Audio rig already exists; inspect before changing it.");
        var canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)
            .FirstOrDefault(c => c.isRootCanvas && c.name == (room ? "MainCanvas" : "MainMenuCanvas"));
        if (canvas == null) throw new InvalidOperationException("Expected scene Canvas was not found.");
        var pause = room ? UnityEngine.Object.FindAnyObjectByType<PauseMenuManager>() : null;
        Transform parent = room ? pause?.editSettingsPanel?.transform.Find("MenuCard_PauseSettings/WhiteBkg/ButtonsPanel") : canvas.transform;
        if (parent == null) throw new InvalidOperationException("Game Settings content was not found.");

        var library = AssetDatabase.LoadAssetAtPath<GameplayAudioLibrary>(LibraryPath);
        if (library == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Audio")) AssetDatabase.CreateFolder("Assets", "Audio");
            library = ScriptableObject.CreateInstance<GameplayAudioLibrary>();
            library.pools = Enum.GetValues(typeof(GameplayAudioPool)).Cast<GameplayAudioPool>()
                .Select(kind => new GameplayAudioLibrary.Pool { kind = kind }).ToArray();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        var root = new GameObject("GameplayAudio");
        Undo.RegisterCreatedObjectUndo(root, "Author gameplay audio");
        var director = root.AddComponent<GameplayAudioDirector>();
        director.library = library;
        director.stadiumActive = room;
        director.homeChatter = Source(root.transform, "HomeChatter");
        director.awayChatter = Source(root.transform, "AwayChatter");
        director.homeChatterNext = Source(root.transform, "HomeChatterNext");
        director.awayChatterNext = Source(root.transform, "AwayChatterNext");
        director.homeChant = Source(root.transform, "HomeChant");
        director.awayChant = Source(root.transform, "AwayChant");
        director.homeReaction = Source(root.transform, "HomeReaction");
        director.awayReaction = Source(root.transform, "AwayReaction");
        director.whistle = Source(root.transform, "Whistle");
        director.gameEffects = Source(root.transform, "GameEffects");
        director.music = Source(root.transform, "Music");

        var panel = Rect("AudioSettings", parent, new Vector2(390, 310));
        Undo.RegisterCreatedObjectUndo(panel.gameObject, "Author audio settings");
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 0.5f);
        panel.anchoredPosition = new Vector2(-24f, 0f);
        var background = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(0.04f, 0.09f, 0.13f, 0.98f);
        var view = panel.gameObject.AddComponent<GameplayAudioSettingsView>();
        ConfigureCheckboxes(view, room, director, pause);
        foreach (var selectable in parent.GetComponentsInChildren<UnityEngine.UI.Selectable>(true))
        {
            if (selectable.GetComponent<GameplayAudioHoverFeedback>() != null) continue;
            var hover = Undo.AddComponent<GameplayAudioHoverFeedback>(selectable.gameObject);
            hover.director = director;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        return scene.path + ": audio rig and three local controls authored; scene requires save_all.";
    }

    /// <summary>Explicit migration: preserve the Main Menu root placement chosen by the designer.</summary>
    public static string UpdateLoadedSettings()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires edit mode.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        bool room = scene.path == "Assets/Scenes/Room.unity";
        if (!room && scene.path != "Assets/Scenes/MainMenu.unity") throw new InvalidOperationException("Only Room or MainMenu is supported.");
        var view = UnityEngine.Object.FindAnyObjectByType<GameplayAudioSettingsView>(FindObjectsInactive.Include);
        var director = UnityEngine.Object.FindAnyObjectByType<GameplayAudioDirector>();
        var pause = room ? UnityEngine.Object.FindAnyObjectByType<PauseMenuManager>(FindObjectsInactive.Include) : null;
        if (view == null || director == null) throw new InvalidOperationException("Inspect the existing audio rig first.");
        ConfigureCheckboxes(view, room, director, pause);
        EditorSceneManager.MarkSceneDirty(scene);
        return scene.path + ": checkbox-only audio settings configured; save through Editor.";
    }

    private static void ConfigureCheckboxes(GameplayAudioSettingsView view, bool room, GameplayAudioDirector director, PauseMenuManager pause)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Buttons/ToggleButton.prefab");
        if (prefab == null) throw new InvalidOperationException("Deck inclusion checkbox prefab is missing.");
        var panel = (RectTransform)view.transform;
        if (room)
        {
            Transform content = pause?.editSettingsPanel?.transform.Find("MenuCard_PauseSettings/WhiteBkg/ButtonsPanel");
            if (content == null) throw new InvalidOperationException("Game Settings content is missing.");
            Undo.SetTransformParent(panel, content, "Move audio into Game Settings");
        }
        // Only replace our own obsolete controls; keep the root and all designer placement.
        Undo.RecordObject(view, "Bind audio checkboxes");
        view.rows = Array.Empty<GameplayAudioSettingsView.Row>();
        foreach (Transform child in panel.Cast<Transform>().ToArray()) Undo.DestroyObjectImmediate(child.gameObject);
        panel.gameObject.layer = LayerMask.NameToLayer("UI");
        var background = panel.GetComponent<UnityEngine.UI.Image>();
        if (background != null)
        {
            Undo.RecordObject(background, "Style audio group background");
            background.enabled = !room;
        }
        Color ink = room ? new Color32(30, 69, 89, 255) : Color.white;
        var title = Label("Title", panel, "AUDIO", Vector2.zero, new Vector2(panel.rect.width - 32, 34));
        title.color = ink;
        Place(title.rectTransform, new Vector2(0, room ? 0 : -12), new Vector2(panel.rect.width - 32, 34));
        title.fontSize = room ? 30 : 23;
        title.alignment = TextAlignmentOptions.MidlineLeft;
        view.rows = new GameplayAudioSettingsView.Row[3];
        string[] labels = { "Stadium sounds", "Game sounds", "Music" };
        for (int i = 0; i < view.rows.Length; i++)
        {
            var row = (GameObject)PrefabUtility.InstantiatePrefab(prefab, panel);
            Undo.RegisterCreatedObjectUndo(row, "Create deck-style audio checkbox");
            PrefabUtility.UnpackPrefabInstance(row, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            row.name = ((GameplayAudioCategory)i) + "AudioRow";
            Place((RectTransform)row.transform, new Vector2(0, -(room ? 44 : 61) - i * (room ? 58 : 76)),
                new Vector2(panel.rect.width - 32, room ? 54 : 62));
            row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().enabled = false;
            Undo.DestroyObjectImmediate(row.transform.Find("Toggle2").gameObject);
            var toggle = row.transform.Find("Toggle1").GetComponent<UnityEngine.UI.Toggle>();
            toggle.name = ((GameplayAudioCategory)i) + "Enabled";
            toggle.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().enabled = false;
            Undo.DestroyObjectImmediate(toggle.transform.Find("Label").gameObject);
            float box = room ? 48 : 36;
            var toggleRect = (RectTransform)toggle.transform;
            toggleRect.anchorMin = toggleRect.anchorMax = toggleRect.pivot = new Vector2(1, 0.5f);
            toggleRect.anchoredPosition = Vector2.zero;
            toggleRect.sizeDelta = new Vector2(box, box);
            var boxRect = (RectTransform)toggle.transform.Find("Background");
            boxRect.anchorMin = Vector2.zero; boxRect.anchorMax = Vector2.one;
            boxRect.offsetMin = boxRect.offsetMax = Vector2.zero;
            var check = (RectTransform)boxRect.Find("Checkmark");
            check.anchorMin = Vector2.zero; check.anchorMax = Vector2.one;
            check.offsetMin = Vector2.one * 4; check.offsetMax = Vector2.one * -4;
            var text = row.transform.Find("Label").GetComponent<TMP_Text>();
            text.text = labels[i]; text.fontSize = room ? 30 : 24; text.color = ink;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = new Vector2(-box - 16, 0);
            var hover = row.GetComponent<GameplayAudioHoverFeedback>() ?? Undo.AddComponent<GameplayAudioHoverFeedback>(toggle.gameObject);
            hover.director = director;
            toggle.SetIsOnWithoutNotify(GameplayAudioPreferences.Local.Enabled((GameplayAudioCategory)i));
            view.rows[i] = new GameplayAudioSettingsView.Row { category = (GameplayAudioCategory)i, enabled = toggle };
        }
        if (room) LayoutRoomSingleRow(view, pause);
        EditorUtility.SetDirty(view);
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
    }

    public static string ArrangeLoadedRoomInOneRow()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires edit mode.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Room.unity") throw new InvalidOperationException("Open Room first.");
        var view = UnityEngine.Object.FindAnyObjectByType<GameplayAudioSettingsView>(FindObjectsInactive.Include);
        var pause = UnityEngine.Object.FindAnyObjectByType<PauseMenuManager>(FindObjectsInactive.Include);
        LayoutRoomSingleRow(view, pause);
        EditorSceneManager.MarkSceneDirty(scene);
        return "Room Audio / Stadium / Game / Music arranged in one row.";
    }

    private static void LayoutRoomSingleRow(GameplayAudioSettingsView view, PauseMenuManager pause)
    {
        var panel = (RectTransform)view.transform;
        Transform content = pause.editSettingsPanel.transform.Find("MenuCard_PauseSettings/WhiteBkg/ButtonsPanel");
        if (panel.parent != content) throw new InvalidOperationException("Audio must be under Game Settings first.");
        Undo.RecordObject(panel, "Arrange audio in one row");
        Place(panel, new Vector2(0, -684), new Vector2(936, 62));
        var title = panel.Find("Title").GetComponent<TMP_Text>();
        Undo.RecordObject(title, "Label audio row");
        Undo.RecordObject(title.rectTransform, "Place audio heading");
        title.text = "Audio";
        Place(title.rectTransform, new Vector2(-388, 0), new Vector2(160, 62));
        float columnWidth = (panel.rect.width - 160 - 3 * 24) / 3;
        string[] labels = { "Stadium", "Game", "Music" };
        for (int i = 0; i < view.rows.Length; i++)
        {
            var row = (RectTransform)view.rows[i].enabled.transform.parent;
            Undo.RecordObject(row, "Place audio checkbox column");
            float x = -panel.rect.width / 2 + 160 + 24 + columnWidth / 2 + i * (columnWidth + 24);
            Place(row, new Vector2(x, 0), new Vector2(columnWidth, 62));
            var label = row.Find("Label").GetComponent<TMP_Text>();
            Undo.RecordObject(label, "Shorten audio category label");
            label.text = labels[i];
        }
        // Reclaim the space used by the old vertical group; preserve other settings.
        var capture = (RectTransform)pause.captureLogFileButton.transform;
        float adjustment = capture.anchoredPosition.y + 776f;
        if (Mathf.Abs(adjustment) > 0.01f)
        {
            var card = (RectTransform)content.parent.parent;
            Undo.RecordObject(card, "Fit settings card to single audio row");
            card.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, card.rect.height + adjustment);
            Undo.RecordObject(content, "Fit settings content to single audio row");
            ((RectTransform)content).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ((RectTransform)content).rect.height + adjustment);
            foreach (RectTransform footer in new[] { capture, pause.logFileStatusText.rectTransform })
            {
                Undo.RecordObject(footer, "Place footer below audio row");
                footer.anchoredPosition += Vector2.down * adjustment;
            }
        }
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static AudioSource Source(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        return source;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.gameObject.layer = parent.gameObject.layer;
        rect.sizeDelta = size;
        return rect;
    }

    private static TextMeshProUGUI Label(string name, Transform parent, string text, Vector2 position, Vector2 size)
    {
        var rect = Rect(name, parent, size);
        rect.anchoredPosition = position;
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 21;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.text = text;
        label.raycastTarget = false;
        return label;
    }
}
