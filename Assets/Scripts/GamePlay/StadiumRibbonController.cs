using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Only updates authored tiles. No light sources or runtime geometry.</summary>
[ExecuteAlways]
public sealed class StadiumRibbonController : MonoBehaviour
{
    [SerializeField] private Renderer[] tiles = Array.Empty<Renderer>();
    private MaterialPropertyBlock block;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    public IReadOnlyList<Renderer> Tiles => tiles;
    public void Configure(Renderer[] authoredTiles) => tiles = authoredTiles;

    public void Apply(bool visible, Color color, float brightness)
    {
        for (int i = 0; i < tiles.Length; i++) SetTile(i, visible, color, brightness);
    }

    public void SetTile(int index, bool visible, Color color, float brightness = 1f)
    {
        if (index < 0 || index >= tiles.Length) throw new ArgumentOutOfRangeException(nameof(index));
        Renderer tile = tiles[index];
        if (tile == null) return;
        tile.enabled = visible;
        block ??= new MaterialPropertyBlock();
        tile.GetPropertyBlock(block);
        block.SetColor(ColorId, color * Mathf.Max(0, brightness));
        tile.SetPropertyBlock(block);
    }
}
