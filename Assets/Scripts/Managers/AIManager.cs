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
        CodexTactician
    }

    public enum CodexTacticianDifficulty
    {
        Easy,
        Medium,
        Hard,
        UltraHard
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
        // A Codex Tactician-controlled match team must also control its own
        // single-player draft.  Draft difficulty does not change the legality
        // or squad-building objective, so all Codex levels use the complete
        // sophisticated roster/formation evaluator rather than falling back to
        // a separate (and potentially human) draft setting.
        string roomPersonaName = string.Equals(currentTeamTurn, "Away", System.StringComparison.OrdinalIgnoreCase)
            ? settings?.awayRoomPersona
            : settings?.homeRoomPersona;
        if (string.IsNullOrWhiteSpace(roomPersonaName))
        {
            roomPersonaName = settings?.defaultRoomPersona;
        }
        if (IsCodexTacticianPersonaName(roomPersonaName))
        {
            return Persona.Sophisticated;
        }

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

        if (TryParseRoomPersona(personaName, out RoomPersona persona))
        {
            return persona;
        }

        return RoomPersona.AskHuman;
    }

    public static CodexTacticianDifficulty ResolveCodexTacticianDifficulty(
        MatchManager.GameSettings settings,
        string expectedTeam)
    {
        string personaName = ResolveRoomPersonaName(settings, expectedTeam);
        if (personaName.IndexOf("ultra", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return CodexTacticianDifficulty.UltraHard;
        }

        if (personaName.IndexOf("easy", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return CodexTacticianDifficulty.Easy;
        }

        if (personaName.IndexOf("hard", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return CodexTacticianDifficulty.Hard;
        }

        return CodexTacticianDifficulty.Medium;
    }

    public static string FormatRoomPersona(RoomPersona persona)
    {
        return persona switch
        {
            RoomPersona.CodexTactician => CodexTacticianProvider.DisplayName,
            _ => persona.ToString()
        };
    }

    private static bool TryParseRoomPersona(string personaName, out RoomPersona persona)
    {
        persona = RoomPersona.AskHuman;
        if (string.IsNullOrWhiteSpace(personaName))
        {
            return false;
        }

        string normalizedPersonaName = personaName.Trim();
        if (IsCodexTacticianPersonaName(normalizedPersonaName))
        {
            persona = RoomPersona.CodexTactician;
            return true;
        }

        // Older saves may still name one of the retired CPU modes. Keep those
        // teams CPU-controlled by routing them through Codex Tactician.
        if (normalizedPersonaName.StartsWith("CPU-Random", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedPersonaName, "Random", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedPersonaName, "1 step", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedPersonaName, "1step", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedPersonaName, "one step", System.StringComparison.OrdinalIgnoreCase)
            || normalizedPersonaName.StartsWith("1 step ", System.StringComparison.OrdinalIgnoreCase)
            || normalizedPersonaName.StartsWith("CPU-1 step", System.StringComparison.OrdinalIgnoreCase))
        {
            persona = RoomPersona.CodexTactician;
            return true;
        }

        return System.Enum.TryParse(normalizedPersonaName, true, out persona);
    }

    public static bool IsCodexTacticianPersonaName(string personaName)
    {
        if (string.IsNullOrWhiteSpace(personaName))
        {
            return false;
        }

        string normalized = personaName.Trim();
        return normalized.StartsWith("Codex Tactician", System.StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("CPU-Codex Tactician", System.StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRoomPersonaName(MatchManager.GameSettings settings, string expectedTeam)
    {
        string personaName = string.Equals(expectedTeam, "Away", System.StringComparison.OrdinalIgnoreCase)
            ? settings?.awayRoomPersona
            : string.Equals(expectedTeam, "Home", System.StringComparison.OrdinalIgnoreCase)
                ? settings?.homeRoomPersona
                : settings?.defaultRoomPersona;
        return string.IsNullOrWhiteSpace(personaName)
            ? settings?.defaultRoomPersona ?? string.Empty
            : personaName;
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
