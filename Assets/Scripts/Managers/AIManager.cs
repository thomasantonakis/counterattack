using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public static AIManager Instance { get; private set; }

    public enum Persona
    {
        Greedy
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

    public string DescribeDraftDecision(GameSettings settings, IEnumerable<Player> visiblePlayers, Player selectedPlayer, string currentTeamTurn)
    {
        Persona persona = ResolveDraftPersona(settings, currentTeamTurn);
        return $"AI manager selected persona '{persona}' for {currentTeamTurn} draft turn. {DraftDecisions.DescribeGreedyOutfielderDecision(visiblePlayers, selectedPlayer)}";
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

    private static DraftDecisions.DraftProfile ResolveDraftProfile(Persona persona)
    {
        return persona switch
        {
            Persona.Greedy => DraftDecisions.DraftProfile.Greedy,
            _ => DraftDecisions.DraftProfile.Greedy
        };
    }
}
