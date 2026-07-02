using System.Linq;
using TMPro;
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
        private const int MaxLineupRows = 18;
        private const float LineupSideColumnWidth = 88f;
        private const float LineupNumberColumnWidth = 24f;

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

        [MenuItem("Tools/CounterAttack/Room/Ensure Match Stats Lineup Rows")]
        public static void EnsureRoomMatchStatsLineupRows()
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

            SerializedObject serializedUi = new(matchStatsUi);
            RectTransform lineupsRoot = serializedUi.FindProperty("lineupsRoot").objectReferenceValue as RectTransform;
            if (lineupsRoot == null)
            {
                Debug.LogError("MatchStatsUI.lineupsRoot is not assigned.");
                return;
            }

            TMP_FontAsset fontAsset = ResolveLineupFont(lineupsRoot);
            SetLegacyLineupColumnsInactive(lineupsRoot);

            RectTransform rowsRoot = EnsureRectTransform(lineupsRoot, "LineupRows");
            rowsRoot.anchorMin = Vector2.zero;
            rowsRoot.anchorMax = Vector2.one;
            rowsRoot.offsetMin = Vector2.zero;
            rowsRoot.offsetMax = Vector2.zero;
            EnsureVerticalLayout(rowsRoot);

            TMP_Text[] homeTexts = new TMP_Text[MaxLineupRows];
            TMP_Text[] numberTexts = new TMP_Text[MaxLineupRows];
            TMP_Text[] awayTexts = new TMP_Text[MaxLineupRows];

            for (int index = 0; index < MaxLineupRows; index++)
            {
                RectTransform row = EnsureRectTransform(rowsRoot, $"LineupRow_{index + 1:00}");
                EnsureRowLayout(row);

                TMP_Text home = EnsureLineupText(row, $"HomeLineup_{index + 1:00}", TextAlignmentOptions.MidlineRight, fontAsset);
                TMP_Text number = EnsureLineupText(row, $"LineupNumber_{index + 1:00}", TextAlignmentOptions.Midline, fontAsset);
                TMP_Text away = EnsureLineupText(row, $"AwayLineup_{index + 1:00}", TextAlignmentOptions.MidlineLeft, fontAsset);

                EnsureRowHoverTarget(row, matchStatsUi, index);
                EnsureLineupLayoutElement(home, LineupSideColumnWidth);
                EnsureLineupLayoutElement(number, LineupNumberColumnWidth);
                EnsureLineupLayoutElement(away, LineupSideColumnWidth);
                homeTexts[index] = home;
                numberTexts[index] = number;
                awayTexts[index] = away;
            }

            AssignTextArray(serializedUi.FindProperty("homeLineupTexts"), homeTexts);
            AssignTextArray(serializedUi.FindProperty("lineupNumberTexts"), numberTexts);
            AssignTextArray(serializedUi.FindProperty("awayLineupTexts"), awayTexts);
            serializedUi.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(matchStatsUi);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Ensured MatchStatsUI lineup rows as edit-mode objects in Room scene.");
        }

        [MenuItem("Tools/CounterAttack/Room/Fill Match Stats Lineup Dummy Text")]
        public static void FillRoomMatchStatsLineupDummyText()
        {
            EnsureRoomMatchStatsLineupRows();

            Scene scene = EditorSceneManager.GetActiveScene();
            MatchStatsUI matchStatsUi = Object.FindObjectsByType<MatchStatsUI>(FindObjectsInactive.Include)
                .FirstOrDefault();
            if (matchStatsUi == null)
            {
                Debug.LogError("Could not find MatchStatsUI in Room scene.");
                return;
            }

            SerializedObject serializedUi = new(matchStatsUi);
            FillLineupDummyText(serializedUi.FindProperty("homeLineupTexts"), "home");
            FillLineupJerseyText(serializedUi.FindProperty("lineupNumberTexts"));
            FillLineupDummyText(serializedUi.FindProperty("awayLineupTexts"), "Away");
            serializedUi.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(matchStatsUi);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Filled MatchStatsUI lineup rows with edit-mode dummy text.");
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

        private static TMP_FontAsset ResolveLineupFont(RectTransform lineupsRoot)
        {
            TMP_Text existingText = lineupsRoot.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => text != null && text.font != null);
            return existingText != null ? existingText.font : TMP_Settings.defaultFontAsset;
        }

        private static void SetLegacyLineupColumnsInactive(RectTransform lineupsRoot)
        {
            SetChildInactive(lineupsRoot, "HomeLineupText");
            SetChildInactive(lineupsRoot, "LineupNumberText");
            SetChildInactive(lineupsRoot, "AwayLineupText");
        }

        private static void SetChildInactive(RectTransform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        private static void EnsureVerticalLayout(RectTransform root)
        {
            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }

        private static void EnsureRowLayout(RectTransform row)
        {
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            LayoutElement layoutElement = row.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = row.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = 14f;
            layoutElement.preferredHeight = 17f;
            layoutElement.flexibleHeight = 1f;
        }

        private static TMP_Text EnsureLineupText(RectTransform parent, string name, TextAlignmentOptions alignment, TMP_FontAsset fontAsset)
        {
            RectTransform rect = EnsureRectTransform(parent, name);
            TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            }

            text.font = fontAsset;
            text.richText = true;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableAutoSizing = true;
            text.fontSizeMin = 8f;
            text.fontSizeMax = 12f;
            text.alignment = alignment;
            text.lineSpacing = 0f;
            text.paragraphSpacing = 0f;
            text.text = string.Empty;
            return text;
        }

        private static void EnsureLineupLayoutElement(TMP_Text text, float columnWidth)
        {
            LayoutElement layoutElement = text.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = text.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minWidth = columnWidth;
            layoutElement.preferredWidth = columnWidth;
            layoutElement.flexibleWidth = 0f;
            layoutElement.minHeight = 14f;
            layoutElement.preferredHeight = 17f;
            layoutElement.flexibleHeight = 1f;
        }

        private static void EnsureRowHoverTarget(RectTransform row, MatchStatsUI owner, int rowIndex)
        {
            Image raycastSurface = row.GetComponent<Image>();
            if (raycastSurface == null)
            {
                raycastSurface = row.gameObject.AddComponent<Image>();
            }

            raycastSurface.color = Color.clear;
            raycastSurface.raycastTarget = true;

            MatchStatsLineupHoverTarget hoverTarget = row.GetComponent<MatchStatsLineupHoverTarget>();
            if (hoverTarget == null)
            {
                hoverTarget = row.gameObject.AddComponent<MatchStatsLineupHoverTarget>();
            }

            hoverTarget.ConfigureRow(owner, rowIndex);
        }

        private static void AssignTextArray(SerializedProperty property, TMP_Text[] values)
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static void FillLineupDummyText(SerializedProperty property, string sideName)
        {
            int rowCount = Mathf.Min(MaxLineupRows, property.arraySize);
            for (int index = 0; index < rowCount; index++)
            {
                TMP_Text text = property.GetArrayElementAtIndex(index).objectReferenceValue as TMP_Text;
                if (text == null)
                {
                    continue;
                }

                text.text = $"{index + 1}.{sideName}";
                EditorUtility.SetDirty(text);
            }
        }

        private static void FillLineupJerseyText(SerializedProperty property)
        {
            int rowCount = Mathf.Min(MaxLineupRows, property.arraySize);
            for (int index = 0; index < rowCount; index++)
            {
                TMP_Text text = property.GetArrayElementAtIndex(index).objectReferenceValue as TMP_Text;
                if (text == null)
                {
                    continue;
                }

                text.text = (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                EditorUtility.SetDirty(text);
            }
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
