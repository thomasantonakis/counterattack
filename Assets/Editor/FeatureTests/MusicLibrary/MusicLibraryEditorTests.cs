using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

[Category("MusicLibrary")]
public sealed class MusicLibraryEditorTests
{
    [Test] public void CatalogContainsEveryImportedMusicAssetWithoutMissingReferences()
    {
        var library = AssetDatabase.LoadAssetAtPath<MusicLibrary>(MusicLibraryAuthoring.LibraryPath);
        Assert.That(library, Is.Not.Null);
        var expected = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Music" })
            .Select(AssetDatabase.GUIDToAssetPath).ToArray();
        Assert.That(library.catalog, Has.None.Null);
        CollectionAssert.AreEquivalent(expected, library.catalog.Select(AssetDatabase.GetAssetPath));
        Assert.That(library.playlists.SelectMany(p => p.tracks), Has.None.Null);
    }

    [Test] public void MatchSceneHasNoAutomaticPlaylist()
    {
        var library = AssetDatabase.LoadAssetAtPath<MusicLibrary>(MusicLibraryAuthoring.LibraryPath);
        Assert.That(library.playlists.SelectMany(p => p.scenes), Does.Not.Contain("Room"));
    }

    [TestCase("MainMenu")]
    [TestCase("CreateNewGameScene")]
    [TestCase("Draft")]
    [TestCase("FreeDraft")]
    public void RequestedScenesHavePlayableMusic(string scene)
    {
        var library = AssetDatabase.LoadAssetAtPath<MusicLibrary>(MusicLibraryAuthoring.LibraryPath);
        var playlist = library.playlists.Single(p => p.scenes.Contains(scene));
        Assert.That(playlist.tracks, Is.Not.Empty);
        Assert.That(playlist.tracks, Has.None.Null);
        Assert.That(playlist.repeat, Is.Not.EqualTo(MusicLibrary.RepeatMode.Once));
    }
}
