#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
using System;
using UnityEngine;

public sealed class DesktopDisplayMode : MonoBehaviour
{
    private Vector2Int windowSize;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void Initialize()
    {
        // Keep explicit display overrides available to standalone proof tooling.
        if (Array.Exists(Environment.GetCommandLineArgs(), argument =>
            argument is "-screen-fullscreen" or "-window-mode")) return;

        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallShortcut()
    {
        var controller = new GameObject(nameof(DesktopDisplayMode));
        DontDestroyOnLoad(controller);
        controller.AddComponent<DesktopDisplayMode>();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F11)) return;

        if (Screen.fullScreenMode == FullScreenMode.Windowed)
        {
            windowSize = new Vector2Int(Screen.width, Screen.height);
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            return;
        }

        RectInt workArea = Screen.mainWindowDisplayInfo.workArea;
        int width = windowSize.x > 0 ? windowSize.x : Mathf.RoundToInt(workArea.width * 0.8f);
        int height = windowSize.y > 0 ? windowSize.y : Mathf.RoundToInt(workArea.height * 0.8f);
        Screen.SetResolution(Mathf.Min(width, workArea.width), Mathf.Min(height, workArea.height), FullScreenMode.Windowed);
    }
}
#endif
