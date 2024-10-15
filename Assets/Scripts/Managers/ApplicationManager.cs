using System.Collections.Generic;
using UnityEngine;

public class ApplicationManager : MonoBehaviour
{
    public static ApplicationManager Instance { get; private set; }
    
    public List<Player> PlayerList { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Prevent this object from being destroyed on scene load
    }

    public void SetPlayerList(List<Player> players)
    {
        PlayerList = players;
    }
}