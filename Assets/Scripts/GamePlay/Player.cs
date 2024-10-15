using System;
using System.Collections.Generic;
using UnityEngine;

public class Player
{
    public string Name { get; set; }
    public string Country { get; set; }
    public int Pace { get; set; }
    public int Dribbling { get; set; }
    public int Heading { get; set; }
    public int HighPass { get; set; }
    public int Resilience { get; set; }
    public int Shooting { get; set; }
    public int Tackling { get; set; }
    public string Type { get; set; }

    // Constructor that takes a dictionary
    public Player(Dictionary<string, string> playerData)
    {
        Name = playerData["Name"];
        Country = playerData["Nationality"];

        // Local variables for parsing
        int pace, dribbling, heading, highPass, resilience, shooting, tackling;

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
        if (int.TryParse(playerData["Heading"], out heading))
        {
            Heading = heading;
        }
        else
        {
            Debug.LogError($"Invalid Heading value for player {Name}, setting default to 0");
            Heading = 0;
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
        if (int.TryParse(playerData["Shooting"], out shooting))
        {
            Shooting = shooting;
        }
        else
        {
            Debug.LogError($"Invalid Shooting value for player {Name}, setting default to 0");
            Shooting = 0;
        }

        // Parse Tackling
        if (int.TryParse(playerData["Tackling"], out tackling))
        {
            Tackling = tackling;
        }
        else
        {
            Debug.LogError($"Invalid Tackling value for player {Name}, setting default to 0");
            Tackling = 0;
        }
    }
}