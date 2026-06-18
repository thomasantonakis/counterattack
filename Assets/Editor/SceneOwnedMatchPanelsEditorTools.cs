#if UNITY_EDITOR
using CounterAttack.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class SceneOwnedMatchPanelsEditorTools
{
    private const string RoomScenePath = "Assets/Scenes/Room.unity";

    [MenuItem("Tools/Counter Attack/Ensure Scene-Owned Match Panels In Room")]
    public static void EnsureSceneOwnedMatchPanelsInRoom()
    {
        EndGamePanelPrefabEditorTools.EnsureSceneInstanceInEditMode();
        PenaltyShootoutOrderPanelPrefabEditorTools.EnsureSceneInstanceInEditMode();
        SubstitutionPanelPrefabEditorTools.EnsureSceneInstanceInEditMode();

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path == RoomScenePath)
        {
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
