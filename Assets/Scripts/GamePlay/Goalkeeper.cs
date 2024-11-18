using System;
using System.Collections.Generic;
using UnityEngine;

public class Goalkeeper
{
    public string Name { get; set; }
    public string Country { get; set; }
    public int Aerial { get; set; }
    public int Dribbling { get; set; }
    public int Pace { get; set; }
    public int Resilience { get; set; }
    public int Saving { get; set; }
    public int Handling { get; set; }
    public int HighPass { get; set; }
    public string Type { get; set; }

    // Constructor that takes a dictionary
    public Goalkeeper(Dictionary<string, string> playerData)
    {
        Name = playerData["Name"];
        Country = playerData["Nationality"];
        Type = playerData["Type"];

        // Local variables for parsing
        int pace, dribbling, aerial, highPass, resilience, saving, handling;

        // Parse Pace
        if (int.TryParse(playerData["Pace"], out pace))
        {
            Pace = pace;
        }
        else
        {
            Debug.LogError($"Invalid Pace value for player {Name}, setting default to 0");
            Pace = 0;
        }

        // Parse Dribbling
        if (int.TryParse(playerData["Dribbling"], out dribbling))
        {
            Dribbling = dribbling;
        }
        else
        {
            Debug.LogError($"Invalid Dribbling value for player {Name}, setting default to 0");
            Dribbling = 0;
        }

        // Parse Heading
        if (int.TryParse(playerData["Aerial"], out aerial))
        {
            Aerial = aerial;
        }
        else
        {
            Debug.LogError($"Invalid Aerial value for player {Name}, setting default to 0");
            Aerial = 0;
        }

        // Parse HighPass
        if (int.TryParse(playerData["HighPass"], out highPass))
        {
            HighPass = highPass;
        }
        else
        {
            Debug.LogError($"Invalid HighPass value for player {Name}, setting default to 0");
            HighPass = 0;
        }

        // Parse Resilience
        if (int.TryParse(playerData["Resilience"], out resilience))
        {
            Resilience = resilience;
        }
        else
        {
            Debug.LogError($"Invalid Resilience value for player {Name}, setting default to 0");
            Resilience = 0;
        }

        // Parse Shooting
        if (int.TryParse(playerData["Saving"], out saving))
        {
            Saving = saving;
        }
        else
        {
            Debug.LogError($"Invalid Saving value for player {Name}, setting default to 0");
            Saving = 0;
        }

        // Parse Tackling
        if (int.TryParse(playerData["Handling"], out handling))
        {
            Handling = handling;
        }
        else
        {
            Debug.LogError($"Invalid Handling value for player {Name}, setting default to 0");
            Handling = 0;
        }
    }

}