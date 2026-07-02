using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public static AIManager Instance { get; private set; }

    public enum Persona
    {
        Greedy,
        Sophisticated
    }

    public enum RoomPersona
    {
        AskHuman,
        Random
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public DraftDecisions.DraftAction GetDraftDecision(GameSettings settings, IEnumerable<Player> visiblePlayers, string currentTeamTurn)
    {
        Persona persona = ResolveDraftPersona(settings, currentTeamTurn);
        DraftDecisions.DraftProfile profile = ResolveDraftProfile(persona);
        return DraftDecisions.ChooseOutfielder(profile, visiblePlayers, currentTeamTurn);
    }

    public DraftDecisions.DraftAction GetDraftDecision(GameSettings settings, DraftDecisions.DraftContext context)
    {
        Persona persona = ResolveDraftPersona(settings, context?.Team);
        DraftDecisions.DraftProfile profile = ResolveDraftProfile(persona);
        return DraftDecisions.ChooseOutfielder(profile, context);
    }

    public string DescribeDraftDecision(GameSettings settings, IEnumerable<Player> visiblePlayers, Player selectedPlayer, string currentTeamTurn)
    {
        Persona persona = ResolveDraftPersona(settings, currentTeamTurn);
        DraftDecisions.DraftProfile profile = ResolveDraftProfile(persona);
        DraftDecisions.DraftContext context = new DraftDecisions.DraftContext(visiblePlayers, null, currentTeamTurn, 0, 0, 0, 0);
        return $"AI manager selected persona '{persona}' for {currentTeamTurn} draft turn. {DraftDecisions.DescribeOutfielderDecision(profile, context, selectedPlayer)}";
    }

    public string DescribeDraftDecision(GameSettings settings, DraftDecisions.DraftContext context, Player selectedPlayer)
    {
        Persona persona = ResolveDraftPersona(settings, context?.Team);
        DraftDecisions.DraftProfile profile = ResolveDraftProfile(persona);
        return $"AI manager selected persona '{persona}' for {context?.Team} draft turn. {DraftDecisions.DescribeOutfielderDecision(profile, context, selectedPlayer)}";
    }

    public Persona ResolveDraftPersona(GameSettings settings, string currentTeamTurn)
    {
        string personaName = string.Equals(currentTeamTurn, "Away", System.StringComparison.OrdinalIgnoreCase)
            ? settings?.awayDraftPersona
            : settings?.homeDraftPersona;

        if (string.IsNullOrWhiteSpace(personaName))
        {
            personaName = settings?.defaultDraftPersona;
        }

        if (System.Enum.TryParse(personaName, true, out Persona persona))
        {
            return persona;
        }

        return Persona.Greedy;
    }

    public static RoomPersona ResolveRoomPersona(MatchManager.GameSettings settings, string expectedTeam)
    {
        string personaName = string.Equals(expectedTeam, "Away", System.StringComparison.OrdinalIgnoreCase)
            ? settings?.awayRoomPersona
            : string.Equals(expectedTeam, "Home", System.StringComparison.OrdinalIgnoreCase)
                ? settings?.homeRoomPersona
                : settings?.defaultRoomPersona;

        if (string.IsNullOrWhiteSpace(personaName))
        {
            personaName = settings?.defaultRoomPersona;
        }

        if (System.Enum.TryParse(personaName, true, out RoomPersona persona))
        {
            return persona;
        }

        return RoomPersona.AskHuman;
    }

    private static DraftDecisions.DraftProfile ResolveDraftProfile(Persona persona)
    {
        return persona switch
        {
            Persona.Greedy => DraftDecisions.DraftProfile.Greedy,
            Persona.Sophisticated => DraftDecisions.DraftProfile.Sophisticated,
            _ => DraftDecisions.DraftProfile.Greedy
        };
    }
}
