using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class DisplayResolutionBootstrap : MonoBehaviour
{
    private const float ScreenCheckIntervalSeconds = 0.25f;
    private static DisplayResolutionBootstrap instance;

    private readonly Dictionary<RectTransform, AnchorSnapshot> canvasChildAnchors = new();
    private Camera clearCamera;
    private readonly Dictionary<CanvasScaler, Vector2> canvasScaleReferences = new();
    private readonly List<CanvasScaler> destroyedScalers = new();
    private int lastScreenWidth;
    private int lastScreenHeight;
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
        DisplaySettings.ApplySavedPreset();
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

    private void Update()
    {
        if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight
            && Time.unscaledTime < nextScreenCheckTime)
        {
            return;
        }

        nextScreenCheckTime = Time.unscaledTime + ScreenCheckIntervalSeconds;
        ApplyDisplayGuards();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyDisplayGuards();
    }

    private void ApplyDisplayGuards()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        Rect viewport = CalculateViewport(lastScreenWidth, lastScreenHeight);
        EnsureClearCamera();
        ApplyCameraViewport(viewport);
        ApplyCanvasSafeArea(viewport);
    }

    public static Rect CalculateViewport(int screenWidth, int screenHeight)
    {
        float aspect = Mathf.Max(1, screenWidth) / (float)Mathf.Max(1, screenHeight);
        const float contentAspect = 16f / 9f;
        float width = Mathf.Min(1f, contentAspect / aspect);
        float height = Mathf.Min(1f, aspect / contentAspect);
        return new Rect((1f - width) / 2f, (1f - height) / 2f, width, height);
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
        foreach (CanvasScaler scaler in canvasScaleReferences.Keys)
        {
            if (scaler == null)
                destroyedScalers.Add(scaler);
        }
        foreach (CanvasScaler scaler in destroyedScalers)
            canvasScaleReferences.Remove(scaler);
        destroyedScalers.Clear();

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (Canvas canvas in canvases)
        {
            if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace
                || (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null))
            {
                continue;
            }

            if (canvas.TryGetComponent(out CanvasScaler scaler)
                && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                if (!canvasScaleReferences.TryGetValue(scaler, out Vector2 reference))
                {
                    reference = scaler.referenceResolution;
                    canvasScaleReferences.Add(scaler, reference);
                }

                // CanvasScaler sees the whole window. Use a reference that yields the same scale
                // as comparing the authored reference resolution against just the content area.
                scaler.referenceResolution = new Vector2(reference.x / viewport.width, reference.y / viewport.height);
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
