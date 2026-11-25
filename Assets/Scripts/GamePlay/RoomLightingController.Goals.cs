using System;
using UnityEngine;

public sealed partial class RoomLightingController
{
    [Header("Goal effects")]
    [SerializeField, Min(.01f)] private float goalColorSwapSeconds = .4f;
    [SerializeField, Min(.01f)] private float goalLightStepSeconds = .15f;
    [SerializeField, Range(0, 1)] private float goalLightDimScale = .2f;
    [SerializeField, Min(0)] private int goalLightDips = 4;
    private bool goalEffectsActive, goalReturning;
    private float goalElapsed, returnElapsed, returnDuration;
    private TokenKitInstructionPalette goalPalette;
    private Renderer[] goalTiles;
    private MaterialPropertyBlock[] originalTileBlocks;
    private bool[] originalTileVisibility;
    private Light[] goalLights;
    private float[] originalLightIntensities;
    public bool GoalEffectsActive => goalEffectsActive;

    public static bool CanCelebrateGoal(FixtureLighting lighting, string matchType, bool scoringTeamIsHome) =>
        (lighting == FixtureLighting.Sunset || lighting == FixtureLighting.LateNight) &&
        (scoringTeamIsHome || string.Equals(matchType, "International", StringComparison.OrdinalIgnoreCase));

    public void BeginGoalEffects(bool scoringTeamIsHome)
    {
        var settings = MatchManager.Instance?.gameData?.gameSettings;
        if (settings == null) return;
        BeginGoalEffects(scoringTeamIsHome, settings.matchType,
            TokenKitCatalog.ResolveInstructionPalette(scoringTeamIsHome ? settings.homeKit : settings.awayKit,
                Color.white, Color.black));
    }

    public void BeginGoalEffects(bool scoringTeamIsHome, string matchType, TokenKitInstructionPalette palette)
    {
        EndGoalEffects();
        if (!CanCelebrateGoal(Applied, matchType, scoringTeamIsHome)) return;
        goalPalette = palette;
        goalTiles = ribbon == null ? Array.Empty<Renderer>() : new Renderer[ribbon.Tiles.Count];
        originalTileBlocks = new MaterialPropertyBlock[goalTiles.Length];
        originalTileVisibility = new bool[goalTiles.Length];
        for (int i = 0; i < goalTiles.Length; i++)
        {
            goalTiles[i] = ribbon.Tiles[i];
            if (goalTiles[i] == null) continue;
            originalTileBlocks[i] = new MaterialPropertyBlock();
            goalTiles[i].GetPropertyBlock(originalTileBlocks[i]);
            originalTileVisibility[i] = goalTiles[i].enabled;
        }
        goalLights = (Light[])floodlights.Clone();
        originalLightIntensities = new float[goalLights.Length];
        for (int i = 0; i < goalLights.Length; i++)
            if (goalLights[i] != null) originalLightIntensities[i] = goalLights[i].intensity;
        goalElapsed = returnElapsed = 0;
        goalReturning = false;
        goalEffectsActive = true;
        AdvanceGoalEffects(0);
    }

    public void BeginGoalReturn(float duration)
    {
        if (!goalEffectsActive) return;
        goalReturning = true;
        returnElapsed = 0;
        returnDuration = Mathf.Max(.001f, duration);
        AdvanceGoalEffects(0);
    }

    // Uses the same scaled clock as the players, so pausing freezes both effects and movement.
    public void AdvanceGoalEffects(float deltaTime)
    {
        if (!goalEffectsActive) return;
        goalElapsed += Mathf.Max(0, deltaTime);
        if (goalReturning) returnElapsed += Mathf.Max(0, deltaTime);
        bool swap = Mathf.FloorToInt(goalElapsed / Mathf.Max(.01f, goalColorSwapSeconds)) % 2 != 0;
        int offset = goalReturning ? Mathf.FloorToInt(Mathf.Clamp01(returnElapsed / returnDuration) * goalTiles.Length) : 0;
        for (int i = 0; i < goalTiles.Length; i++)
        {
            bool secondary = goalReturning ? IsChaseTile(i, goalTiles.Length, offset) : ((i / 5) % 2 != 0) ^ swap;
            ribbon.SetTile(i, originalTileVisibility[i], secondary ? goalPalette.Secondary : goalPalette.Primary);
        }
        int step = Mathf.FloorToInt(goalElapsed / Mathf.Max(.01f, goalLightStepSeconds));
        float scale = step < goalLightDips * 2 && step % 2 == 0 ? goalLightDimScale : 1;
        for (int i = 0; i < goalLights.Length; i++)
            if (goalLights[i] != null) goalLights[i].intensity = originalLightIntensities[i] * scale;
    }

    public static bool IsChaseTile(int index, int count, int offset)
    {
        if (count <= 0) return false;
        for (int group = 0; group < 5; group++)
        {
            int start = group * count / 5 + offset;
            int distance = ((index - start) % count + count) % count;
            if (distance < 3) return true;
        }
        return false;
    }

    public void EndGoalEffects()
    {
        if (!goalEffectsActive) return;
        goalEffectsActive = false;
        for (int i = 0; i < goalTiles.Length; i++)
            if (goalTiles[i] != null)
            {
                goalTiles[i].SetPropertyBlock(originalTileBlocks[i]);
                goalTiles[i].enabled = originalTileVisibility[i];
            }
        for (int i = 0; i < goalLights.Length; i++)
            if (goalLights[i] != null) goalLights[i].intensity = originalLightIntensities[i];
    }
}
