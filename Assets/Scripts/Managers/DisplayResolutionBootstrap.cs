using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DisplayResolutionBootstrap : MonoBehaviour
{
    private const float ScreenCheckIntervalSeconds = 0.25f;
    private static DisplayResolutionBootstrap instance;

    private readonly Dictionary<RectTransform, AnchorSnapshot> canvasChildAnchors = new();
    private Camera clearCamera;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private Rect currentViewport = new(0f, 0f, 1f, 1f);
    private float nextScreenCheckTime;

    private readonly struct AnchorSnapshot
    {
        public readonly Vector2 anchorMin;
        public readonly Vector2 anchorMax;

        public AnchorSnapshot(RectTransform rectTransform)
        {
            anchorMin = rectTransform.anchorMin;
            anchorMax = rectTransform.anchorMax;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject bootstrapObject = new(nameof(DisplayResolutionBootstrap));
        instance = bootstrapObject.AddComponent<DisplayResolutionBootstrap>();
        DontDestroyOnLoad(bootstrapObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ConfigureBuildResolution();
        EnsureClearCamera();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void Start()
    {
        ApplyDisplayGuards(force: true);
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScreenCheckTime)
        {
            return;
        }

        nextScreenCheckTime = Time.unscaledTime + ScreenCheckIntervalSeconds;
        ApplyDisplayGuards(force: false);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyDisplayGuards(force: true);
    }

    private static void ConfigureBuildResolution()
    {
#if !UNITY_EDITOR
        Resolution nativeResolution = Screen.currentResolution;
        int width = nativeResolution.width > 0 ? nativeResolution.width : Display.main.systemWidth;
        int height = nativeResolution.height > 0 ? nativeResolution.height : Display.main.systemHeight;

        if (width > 0 && height > 0)
        {
            Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
        }
#endif
    }

    private void ApplyDisplayGuards(bool force)
    {
        int screenWidth = Mathf.Max(1, Screen.width);
        int screenHeight = Mathf.Max(1, Screen.height);

        lastScreenWidth = screenWidth;
        lastScreenHeight = screenHeight;
        currentViewport = CalculateViewport(screenWidth, screenHeight);
        EnsureClearCamera();
        ApplyCameraViewport(currentViewport);
        ApplyCanvasSafeArea(currentViewport);
    }

    private static Rect CalculateViewport(int screenWidth, int screenHeight)
    {
        return new Rect(0f, 0f, 1f, 1f);
    }

    private static void ApplyCameraViewport(Rect viewport)
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include);
        foreach (Camera targetCamera in cameras)
        {
            if (targetCamera == null
                || targetCamera.targetTexture != null
                || targetCamera.GetComponent<DisplayClearCameraMarker>() != null)
            {
                continue;
            }

            targetCamera.rect = viewport;
        }
    }

    private void EnsureClearCamera()
    {
        if (clearCamera != null)
        {
            clearCamera.rect = new Rect(0f, 0f, 1f, 1f);
            return;
        }

        GameObject clearCameraObject = new("Display Clear Camera");
        clearCameraObject.transform.SetParent(transform, false);
        clearCameraObject.AddComponent<DisplayClearCameraMarker>();

        clearCamera = clearCameraObject.AddComponent<Camera>();
        clearCamera.clearFlags = CameraClearFlags.SolidColor;
        clearCamera.backgroundColor = Color.black;
        clearCamera.cullingMask = 0;
        clearCamera.depth = -10000f;
        clearCamera.rect = new Rect(0f, 0f, 1f, 1f);
        clearCamera.allowHDR = false;
        clearCamera.allowMSAA = false;
    }

    private void ApplyCanvasSafeArea(Rect viewport)
    {
        CleanupDestroyedCanvasChildren();

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)
            {
                continue;
            }

            ApplyCanvasChildrenSafeArea(canvas.transform, viewport);
        }
    }

    private void CleanupDestroyedCanvasChildren()
    {
        List<RectTransform> destroyedKeys = null;
        foreach (RectTransform rectTransform in canvasChildAnchors.Keys)
        {
            if (rectTransform == null)
            {
                destroyedKeys ??= new List<RectTransform>();
                destroyedKeys.Add(rectTransform);
            }
        }

        if (destroyedKeys == null)
        {
            return;
        }

        foreach (RectTransform destroyedKey in destroyedKeys)
        {
            canvasChildAnchors.Remove(destroyedKey);
        }
    }

    private void ApplyCanvasChildrenSafeArea(Transform canvasTransform, Rect viewport)
    {
        for (int index = 0; index < canvasTransform.childCount; index++)
        {
            if (canvasTransform.GetChild(index) is not RectTransform childRect)
            {
                continue;
            }

            if (!canvasChildAnchors.TryGetValue(childRect, out AnchorSnapshot snapshot))
            {
                snapshot = new AnchorSnapshot(childRect);
                canvasChildAnchors.Add(childRect, snapshot);
            }

            childRect.anchorMin = MapAnchorIntoViewport(snapshot.anchorMin, viewport);
            childRect.anchorMax = MapAnchorIntoViewport(snapshot.anchorMax, viewport);
        }
    }

    private static Vector2 MapAnchorIntoViewport(Vector2 anchor, Rect viewport)
    {
        return new Vector2(
            viewport.xMin + (anchor.x * viewport.width),
            viewport.yMin + (anchor.y * viewport.height));
    }

    private sealed class DisplayClearCameraMarker : MonoBehaviour
    {
    }
}
