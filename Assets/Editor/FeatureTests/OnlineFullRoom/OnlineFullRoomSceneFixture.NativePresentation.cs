using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using Newtonsoft.Json;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class OnlineFullRoomSceneFixture
{
    private IEnumerator LoadNativeRoom(MatchManager.GameData data, bool guest, string matchId)
    {
        string path = Path.Combine(directory, guest ? "native-replica.json" : "native-authority.json");
        File.WriteAllText(path, JsonConvert.SerializeObject(data));
        session = new OnlineRoomSession(matchId, guest, data, path);
        app.SetActiveSaveFilePath(path);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
            $"Assets/Scenes/{OnlineRoomStartup.SceneName}.unity", new LoadSceneParameters(LoadSceneMode.Single));
        var startup = Object.FindAnyObjectByType<OnlineRoomStartup>();
        Assert.That(startup, Is.Not.Null);
        float deadline = Time.realtimeSinceStartup + 15f;
        while (startup.Board == null && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(startup.Board, Is.Not.Null, "Room must bind the online role");
    }

    private IEnumerator LoadPresentationGuest(MatchManager.GameData data, string matchId, bool nativeRoom)
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity", new LoadSceneParameters(LoadSceneMode.Single));
        session.Dispose();
        session = null;
        MatchManager.Instance = null;
        yield return nativeRoom ? LoadNativeRoom(data, true, matchId) : Load(data, true);
    }

    public IEnumerator RunNativeProjection(string presentation)
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, false);
        var version = new OnlineCompatibility("ground-wire-v7", "ground-rules-v1", OnlineFullRoomBootstrap.Catalog);
        var protocol = new OnlineMatchProtocol("native-room-scene", "home", "away", "away", version);
        protocol.BindAuthenticatedAccount("home", "h", version);
        protocol.BindAuthenticatedAccount("away", "a", version);
        yield return LoadNativeRoom(data, false, protocol.MatchId);
        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        pause = new OnlineSharedPause(protocol, board.match);
        yield return presentation switch
        {
            "stoppage" => RunSharedRollPresentation(board, protocol, data, timeout: false, nativeRoom: true),
            "duel" => RunSharedDuelOrHeader(board, protocol, data, header: false, nativeRoom: true),
            "header" => RunSharedDuelOrHeader(board, protocol, data, header: true, nativeRoom: true),
            "half-time" => RunNormalOnlineHalf(board, protocol, 1),
            _ => RunNativeMovementProjection(board, protocol, data)
        };
    }

    private IEnumerator RunNativeMovementProjection(OnlineGroundPassBoard board, OnlineMatchProtocol protocol, MatchManager.GameData data)
    {
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        var movement = board.match.movementPhaseManager;
        movement.ActivateMovementPhase();
        var open = pause.Capture(authority);
        Assert.That(open.movementPanel?.selected, Is.Null);
        Assert.That(open.movementPanel?.attacking, Is.Empty);
        yield return null;

        var panel = Object.FindAnyObjectByType<MovementPhaseMovedTokensPanel>();
        var slot = ActiveMovementSlot(panel);
        var teamFace = slot.GetComponentInChildren<RawImage>(true).texture;
        Assert.That(teamFace, Is.Not.Null);
        Assert.That(SlotNumber(slot), Is.Empty);
        var player = movement.GetSelectableMovementTokensForDecisionPreview().First(token => !token.IsGoalKeeper);
        Assert.That(movement.TryExecuteOnlineMovement(OnlineMovementIntent.SelectPlayer, player), Is.True);
        yield return null;
        var selected = pause.Capture(authority);
        Assert.That(selected.movementPanel?.selected, Is.EqualTo(OnlineRollPublication.ActorId(player)));
        var number = player.jerseyNumber.ToString();
        Assert.That(SlotNumber(ActiveMovementSlot(panel)), Is.EqualTo(number));

        pause.Dispose();
        pause = null;
        yield return LoadPresentationGuest(data, protocol.MatchId, nativeRoom: true);
        board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        panel = Object.FindAnyObjectByType<MovementPhaseMovedTokensPanel>();
        board.Display(open, "guest");
        yield return null;
        slot = ActiveMovementSlot(panel);
        var face = slot.GetComponentInChildren<RawImage>(true);
        Assert.That(face.texture, Is.SameAs(teamFace));
        Assert.That(SlotNumber(slot), Is.Empty);
        float initialAlpha = face.color.a;
        float deadline = Time.realtimeSinceStartup + 1.5f;
        while (Mathf.Abs(face.color.a - initialAlpha) < .03f && Time.realtimeSinceStartup < deadline)
        {
            board.Display(open, "guest");
            yield return null;
        }
        Assert.That(Mathf.Abs(face.color.a - initialAlpha), Is.GreaterThanOrEqualTo(.03f),
            "The guest pulse advances through repeated state updates");
        Assert.That(board.match.IsGameplayInputBlocked, Is.True);
        board.Display(selected, "guest");
        Assert.That(SlotNumber(ActiveMovementSlot(panel)), Is.EqualTo(number));
    }

    private static MovementPhaseMovedTokenSlotView ActiveMovementSlot(MovementPhaseMovedTokensPanel panel)
    {
        Assert.That(panel, Is.Not.Null);
        Assert.That(panel.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
        return panel.GetComponentsInChildren<MovementPhaseMovedTokenSlotView>(true)
            .Single(slot => slot.State == MovementPhaseMovedTokenSlotView.PresentationState.Active);
    }

    private static string SlotNumber(MovementPhaseMovedTokenSlotView slot)
    {
        var text = (TMP_Text)typeof(MovementPhaseMovedTokenSlotView)
            .GetField("numberText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(slot);
        return text.text;
    }
}
