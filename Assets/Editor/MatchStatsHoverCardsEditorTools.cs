using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CounterAttack.EditorTools
{
    public static class MatchStatsHoverCardsEditorTools
    {
        private const string RoomScenePath = "Assets/Scenes/Room.unity";
        private const string PlayerCardPrefabPath = "Assets/Resources/UI/PlayerCardPrefab.prefab";
        private const string GoalkeeperCardPrefabPath = "Assets/Resources/UI/GoalKeeperCardPrefab.prefab";
        private const string RunOnceMarkerPath = "Assets/Editor/MatchStatsHoverCardsEditorTools.runonce";
        private const float SidePaddingRatio = 0.03f;
        private const float CardColumnGap = 0.035f;

        [InitializeOnLoadMethod]
        private static void RunOnceAfterReload()
        {
            if (!System.IO.File.Exists(RunOnceMarkerPath))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    Debug.LogWarning("Match stats hover card setup is pending until Unity returns to Edit Mode.");
                    return;
                }

                EnsureRoomMatchStatsHoverCards();
                AssetDatabase.DeleteAsset(RunOnceMarkerPath);
                AssetDatabase.Refresh();
            };
        }

        [MenuItem("Tools/CounterAttack/Room/Ensure Match Stats Hover Cards")]
        public static void EnsureRoomMatchStatsHoverCards()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (!string.Equals(scene.path, RoomScenePath, System.StringComparison.Ordinal))
            {
                scene = EditorSceneManager.OpenScene(RoomScenePath, OpenSceneMode.Single);
            }

            MatchStatsUI matchStatsUi = Object.FindObjectsByType<MatchStatsUI>(FindObjectsInactive.Include)
                .FirstOrDefault();
            if (matchStatsUi == null)
            {
                Debug.LogError("Could not find MatchStatsUI in Room scene.");
                return;
            }

            PlayerCard playerCardPrefab = AssetDatabase.LoadAssetAtPath<PlayerCard>(PlayerCardPrefabPath);
            GoalkeeperCard goalkeeperCardPrefab = AssetDatabase.LoadAssetAtPath<GoalkeeperCard>(GoalkeeperCardPrefabPath);
            if (playerCardPrefab == null || goalkeeperCardPrefab == null)
            {
                Debug.LogError("Could not load match stats hover card prefabs from Assets/Resources/UI.");
                return;
            }

            SerializedObject serializedUi = new(matchStatsUi);
            RectTransform panel = serializedUi.FindProperty("panel").objectReferenceValue as RectTransform;
            if (panel == null)
            {
                Debug.LogError("MatchStatsUI.panel is not assigned.");
                return;
            }

            float cardsBottomPadding = serializedUi.FindProperty("cardsBottomPadding").floatValue;
            float cardsTopAnchor = serializedUi.FindProperty("cardsTopAnchor").floatValue;
            RectTransform root = EnsureRectTransform(panel, "HoverCardsRoot");
            root.anchorMin = new Vector2(SidePaddingRatio, cardsBottomPadding);
            root.anchorMax = new Vector2(1f - SidePaddingRatio, cardsTopAnchor);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            RectTransform homeAnchor = EnsureRectTransform(root, "HomeHoverCardAnchor");
            homeAnchor.anchorMin = new Vector2(0f, 0f);
            homeAnchor.anchorMax = new Vector2(0.5f - (CardColumnGap * 0.5f), 1f);
            homeAnchor.offsetMin = Vector2.zero;
            homeAnchor.offsetMax = Vector2.zero;
            EnsureMask(homeAnchor);

            RectTransform awayAnchor = EnsureRectTransform(root, "AwayHoverCardAnchor");
            awayAnchor.anchorMin = new Vector2(0.5f + (CardColumnGap * 0.5f), 0f);
            awayAnchor.anchorMax = new Vector2(1f, 1f);
            awayAnchor.offsetMin = Vector2.zero;
            awayAnchor.offsetMax = Vector2.zero;
            EnsureMask(awayAnchor);

            PlayerCard homeOutfield = EnsureCardInstance(playerCardPrefab, homeAnchor, "HomeOutfieldHoverCard");
            PlayerCard awayOutfield = EnsureCardInstance(playerCardPrefab, awayAnchor, "AwayOutfieldHoverCard");
            GoalkeeperCard homeGoalkeeper = EnsureCardInstance(goalkeeperCardPrefab, homeAnchor, "HomeGoalkeeperHoverCard");
            GoalkeeperCard awayGoalkeeper = EnsureCardInstance(goalkeeperCardPrefab, awayAnchor, "AwayGoalkeeperHoverCard");

            serializedUi.FindProperty("playerCardPrefab").objectReferenceValue = playerCardPrefab;
            serializedUi.FindProperty("goalkeeperCardPrefab").objectReferenceValue = goalkeeperCardPrefab;
            serializedUi.FindProperty("hoverCardsRoot").objectReferenceValue = root;
            serializedUi.FindProperty("homeHoverCardAnchor").objectReferenceValue = homeAnchor;
            serializedUi.FindProperty("awayHoverCardAnchor").objectReferenceValue = awayAnchor;
            serializedUi.FindProperty("homeHoverCard").objectReferenceValue = homeOutfield;
            serializedUi.FindProperty("awayHoverCard").objectReferenceValue = awayOutfield;
            serializedUi.FindProperty("homeGoalkeeperHoverCard").objectReferenceValue = homeGoalkeeper;
            serializedUi.FindProperty("awayGoalkeeperHoverCard").objectReferenceValue = awayGoalkeeper;
            serializedUi.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(matchStatsUi);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Ensured MatchStatsUI hover cards as edit-mode prefab instances in Room scene.");
        }

        private static RectTransform EnsureRectTransform(RectTransform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null && existing is RectTransform existingRect)
            {
                return existingRect;
            }

            GameObject child = new(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static void EnsureMask(RectTransform rectTransform)
        {
            if (!rectTransform.TryGetComponent<RectMask2D>(out _))
            {
                rectTransform.gameObject.AddComponent<RectMask2D>();
            }
        }

        private static T EnsureCardInstance<T>(T prefab, RectTransform parent, string name) where T : Component
        {
            Transform existing = parent.Find(name);
            T existingCard = existing != null ? existing.GetComponent<T>() : null;
            if (existingCard != null)
            {
                existingCard.gameObject.SetActive(false);
                return existingCard;
            }

            GameObject cardObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, parent);
            cardObject.name = name;
            cardObject.SetActive(false);
            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.5f, 0f);
                cardRect.anchorMax = new Vector2(0.5f, 0f);
                cardRect.pivot = new Vector2(0.5f, 0f);
                cardRect.anchoredPosition = new Vector2(0f, 6f);
            }

            return cardObject.GetComponent<T>();
        }
    }
}
