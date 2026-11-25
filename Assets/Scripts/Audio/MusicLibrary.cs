using System;
using UnityEngine;

/// <summary>Editable music catalog and scene assignments. Clips use their imported asset names.</summary>
[CreateAssetMenu(menuName = "Counter Attack/Music Library")]
public sealed class MusicLibrary : ScriptableObject
{
    public enum RepeatMode { Once, RepeatTrack, RepeatPlaylist }
    [Serializable] public sealed class Playlist
    {
        public string label;
        public string[] scenes = Array.Empty<string>();
        public AudioClip[] tracks = Array.Empty<AudioClip>();
        public RepeatMode repeat = RepeatMode.Once;
    }

    public AudioClip[] catalog = Array.Empty<AudioClip>();
    public Playlist[] playlists = Array.Empty<Playlist>();
    [Range(0f, 1f)] public float musicVolume = 0.65f;
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Min(0f)] public float fadeSeconds = 1f;
}
