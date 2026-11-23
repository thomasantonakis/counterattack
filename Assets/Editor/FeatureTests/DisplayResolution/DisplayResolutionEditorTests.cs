using NUnit.Framework;
using UnityEngine;

public sealed class DisplayResolutionEditorTests
{
    [TestCase(3840, 2160, 0f, 0f, 1f, 1f)]
    [TestCase(1920, 1200, 0f, 0.05f, 1f, 0.9f)]
    [TestCase(2560, 1080, 0.125f, 0f, 0.75f, 1f)]
    [TestCase(720, 1280, 0f, 0.341796875f, 1f, 0.31640625f)]
    public void FitsCenteredSixteenByNineContent(int width, int height, float x, float y,
        float viewportWidth, float viewportHeight)
    {
        Rect viewport = DisplayResolutionBootstrap.CalculateViewport(width, height);
        Assert.That(viewport.x, Is.EqualTo(x).Within(0.0001f));
        Assert.That(viewport.y, Is.EqualTo(y).Within(0.0001f));
        Assert.That(viewport.width, Is.EqualTo(viewportWidth).Within(0.0001f));
        Assert.That(viewport.height, Is.EqualTo(viewportHeight).Within(0.0001f));
    }

    [Test]
    public void WindowedPresetsFitBothWorkAreaDimensions()
    {
        var presets = DisplaySettings.AvailablePresets(new Vector2Int(1920, 1080), new Vector2Int(1904, 1032));
        CollectionAssert.AreEqual(new[]
        {
            (FullScreenMode.FullScreenWindow, Vector2Int.zero),
            (FullScreenMode.FullScreenWindow, new Vector2Int(1280, 720)),
            (FullScreenMode.Windowed, new Vector2Int(1280, 720))
        }, presets);
    }

    [Test]
    public void ReducedFullscreenPresetsUseDisplayBoundsEvenWhenNoWindowFits()
    {
        var presets = DisplaySettings.AvailablePresets(new Vector2Int(2880, 1800), new Vector2Int(1279, 720));
        CollectionAssert.AreEqual(new[]
        {
            (FullScreenMode.FullScreenWindow, Vector2Int.zero),
            (FullScreenMode.FullScreenWindow, new Vector2Int(1280, 720)),
            (FullScreenMode.FullScreenWindow, new Vector2Int(1920, 1080)),
            (FullScreenMode.FullScreenWindow, new Vector2Int(2560, 1440))
        }, presets);
    }
}
