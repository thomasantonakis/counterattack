using System;
using System.Linq;
using CounterAttack.Online;
using Unity.Pipeline.Commands;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OnlineRoomStartupAuthoring
{
    [CliCommand("author_online_room_startup", "Author explicit guest authority-root references in the loaded clean Room scene")]
    public static string Prepare()
    {
        var scene = SceneManager.GetActiveScene();
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/Room.unity" || scene.isDirty)
            throw new InvalidOperationException("Expected clean stopped Room");
        var gate = UnityEngine.Object.FindAnyObjectByType<OnlineRoomStartup>();
        if (gate == null) gate = new GameObject("Online Room startup role").AddComponent<OnlineRoomStartup>();
        var names = new[] { "HexGrid", "MatchBall", "Player Tokens", "MatchManager", "GameInputManager",
            "MovementPhaseManager", "GroundBallManager", "FirstTimePassManager", "HighPassManager", "HeaderManager",
            "LongBallManager", "LooseBallManager", "OutOfBoundsManager", "FreeKickManager", "ShotManager",
            "FinalThirdManager", "GoalFlowManager", "KickoffManager", "GoalKeeperManager", "GameDebugMonitor",
            "HelperFunctions", "ThrowInManager", "GKPushManager", "OOBPushManager", "OffsideManager",
            "PenaltyKickManager", "PenaltyShootoutManager", "RoomDecisionManager" };
        var roots = scene.GetRootGameObjects();
        gate.authorityRoots = names.Select(name => roots.Single(root => root.name == name)).ToArray();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Room startup references");
        return scene.path;
    }
}
