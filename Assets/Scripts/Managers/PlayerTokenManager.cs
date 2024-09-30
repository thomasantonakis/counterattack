using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTokenManager : MonoBehaviour
{
    public GameObject homePlayerPrefab;  // Prefab for Home team players (red)
    public GameObject awayPlayerPrefab;  // Prefab for Away team players (blue)
    
    // Spawn positions for the players
    private Vector3[] homeTeamPositions = new Vector3[]
    {
        new Vector3(-5, 0, 0), new Vector3(-6, 0, 1), new Vector3(-7, 0, -1),
        new Vector3(-8, 0, 2), new Vector3(-9, 0, -2), new Vector3(-10, 0, 1),
        new Vector3(-11, 0, -1), new Vector3(-12, 0, 0), new Vector3(-13, 0, 1),
        new Vector3(-14, 0, -1)
    };

    private Vector3[] awayTeamPositions = new Vector3[]
    {
        new Vector3(5, 0, 0), new Vector3(6, 0, 1), new Vector3(7, 0, -1),
        new Vector3(8, 0, 2), new Vector3(9, 0, -2), new Vector3(10, 0, 1),
        new Vector3(11, 0, -1), new Vector3(12, 0, 0), new Vector3(13, 0, 1),
        new Vector3(14, 0, -1)
    };

    void Start()
    {
        SpawnTeamTokens(homePlayerPrefab, homeTeamPositions);  // Spawn Home team tokens
        SpawnTeamTokens(awayPlayerPrefab, awayTeamPositions);  // Spawn Away team tokens
    }

    // Method to spawn player tokens at specific positions
    void SpawnTeamTokens(GameObject prefab, Vector3[] positions)
    {
        foreach (var position in positions)
        {
            Instantiate(prefab, position, Quaternion.identity);
        }
    }
}
