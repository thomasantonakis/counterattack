using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Counter Attack/Gameplay Audio Library")]
public sealed class GameplayAudioLibrary : ScriptableObject
{
    [Serializable]
    public sealed class Pool
    {
        public GameplayAudioPool kind;
        public AudioClip[] clips = Array.Empty<AudioClip>();
    }

    [Tooltip("Assign cleared/licensed clips here. Empty pools intentionally stay silent.")]
    public Pool[] pools = Array.Empty<Pool>();

    public AudioClip[] Get(GameplayAudioPool kind)
    {
        foreach (Pool pool in pools)
            if (pool != null && pool.kind == kind)
                return pool.clips ?? Array.Empty<AudioClip>();
        return Array.Empty<AudioClip>();
    }
}
