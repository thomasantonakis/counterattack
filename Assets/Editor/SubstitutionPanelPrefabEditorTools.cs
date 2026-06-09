#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CounterAttack.Editor
{
    public static class SubstitutionPanelPrefabEditorTools
    {
        private const string ResourcesFolder = "Assets/Resources";
        private const string UiResourcesFolder = "Assets/Resources/UI";
        private const string PrefabPath = UiResourcesFolder + "/SubstitutionPanel.prefab";

        private static readonly Color PanelColor = new(0.05f, 0.06f, 0.07f, 0.94f);
        private static readonly Color ColumnColor = new(0.12f, 0.13f, 0.15f, 0.96f);
        private static readonly Color ButtonColor = new(0.88f, 0.88f, 0.88f, 1f);
        private static readonly Color TextColor = new(0.94f, 0.94f, 0.94f, 1f);
        private static readonly Color MutedTextColor = new(0.66f, 0.68f, 0.7f, 1f);
        private static readonly Color OutDropdownColor = new(0.36f, 0.08f, 0.08f, 1f);
        private static readonly Color InDropdownColor = new(0.08f, 0.28f, 0.13f, 1f);

        [MenuItem("CounterAttack/Room/Rebuild Substitution Panel Prefab")]
        public static void RebuildSubstitutionPanelPrefab()
        {
            EnsureFolder(ResourcesFolder);
            EnsureFolder(UiResourcesFolder);

            GameObject root = CreateRect("SubstitutionPanel", null, typeof(Image));
            try
            {
                SubstitutionMenuView view = root.AddComponent<SubstitutionMenuView>();
                Image panelImage = root.GetComponent<Image>();
                panelImage.color = PanelColor;
                panelImage.raycastTarget = true;
                Stretch(root.GetComponent<RectTransform>());

                VerticalLayoutGroup rootLayout = root.AddComponent<VerticalLayoutGroup>();
                rootLayout.padding = new RectOffset(18, 18, 12, 12);
                rootLayout.spacing = 8;
                rootLayout.childControlWidth = true;
                rootLayout.childControlHeight = true;
                rootLayout.childForceExpandWidth = true;
                rootLayout.childForceExpandHeight = false;

                view.titleText = CreateText("Title", root.transform, "Substitutions", 28f, TextColor, TextAlignmentOptions.Center);
                view.titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

                GameObject body = CreateRect("Body", root.transform);
                LayoutElement bodyLayout = body.AddComponent<LayoutElement>();
                bodyLayout.flexibleHeight = 1f;
                bodyLayout.minHeight = 0f;
                HorizontalLayoutGroup bodyGroup = body.AddComponent<HorizontalLayoutGroup>();
                bodyGroup.spacing = 12;
                bodyGroup.childControlWidth = true;
                bodyGroup.childControlHeight = true;
                bodyGroup.childForceExpandWidth = true;
                bodyGroup.childForceExpandHeight = true;

                BuildTeamColumn(body.transform, view, true);
                BuildTeamColumn(body.transform, view, false);
                BuildFooter(root.transform, view);
                ApplyDesignerEditableLayout(root.transform, view);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Substitution panel prefab rebuilt at {PrefabPath}.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildTeamColumn(Transform parent, SubstitutionMenuView view, bool isHomeTeam)
        {
            GameObject column = CreateRect(isHomeTeam ? "HomeSubstitutionsColumn" : "AwaySubstitutionsColumn", parent, typeof(Image));
            column.GetComponent<Image>().color = ColumnColor;
            LayoutElement columnLayout = column.AddComponent<LayoutElement>();
            columnLayout.flexibleWidth = 1f;
            columnLayout.minWidth = 360f;

            VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI title = CreateText("TeamTitle", column.transform, isHomeTeam ? "Home Team" : "Away Team", 20f, TextColor, TextAlignmentOptions.Center);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

            GameObject rosterArea = CreateRect("RosterArea", column.transform);
            LayoutElement rosterLayout = rosterArea.AddComponent<LayoutElement>();
            rosterLayout.preferredHeight = 220f;
            rosterLayout.flexibleHeight = 1f;

            HorizontalLayoutGroup rosterGroup = rosterArea.AddComponent<HorizontalLayoutGroup>();
            rosterGroup.spacing = 12;
            rosterGroup.childControlWidth = true;
            rosterGroup.childControlHeight = true;
            rosterGroup.childForceExpandWidth = true;
            rosterGroup.childForceExpandHeight = true;

            Transform playingContent = CreateRosterListShell(rosterArea.transform, "Playing");
            Transform benchContent = CreateRosterListShell(rosterArea.transform, "Bench");
            AddSampleRosterRows(playingContent, 1, 11);
            AddSampleRosterRows(benchContent, 12, 13);

            TextMeshProUGUI remaining = CreateText("SubsRemaining", column.transform, "3 subs remaining", 15f, MutedTextColor, TextAlignmentOptions.Center);
            remaining.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

            GameObject rowsContainer = CreateRect("SubstitutionRows", column.transform);
            rowsContainer.AddComponent<LayoutElement>().preferredHeight = 140f;
            VerticalLayoutGroup rowsLayout = rowsContainer.AddComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 4;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = false;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;

            SubstitutionDropdownRowView[] rows = new SubstitutionDropdownRowView[MatchManager.ExtraTimeMaxSubstitutionsPerTeam];
            for (int index = 0; index < rows.Length; index++)
            {
                rows[index] = CreateDropdownRow(rowsContainer.transform, isHomeTeam, index);
            }

            if (isHomeTeam)
            {
                view.homeTeamTitleText = title;
                view.homePlayingListContent = playingContent;
                view.homeBenchListContent = benchContent;
                view.homeSubsRemainingText = remaining;
                view.homeSubstitutionRowsContainer = rowsContainer.transform;
                view.homeRows = rows;
            }
            else
            {
                view.awayTeamTitleText = title;
                view.awayPlayingListContent = playingContent;
                view.awayBenchListContent = benchContent;
                view.awaySubsRemainingText = remaining;
                view.awaySubstitutionRowsContainer = rowsContainer.transform;
                view.awayRows = rows;
            }
        }

        private static void ApplyDesignerEditableLayout(Transform root, SubstitutionMenuView view)
        {
            DestroyLayoutDriver(root.gameObject);

            RectTransform title = view.titleText.GetComponent<RectTransform>();
            SetStretchTop(title, 0f, 0f, 34f);

            RectTransform body = root.Find("Body") as RectTransform;
            DestroyLayoutDriver(body.gameObject);
            SetStretch(body, 18f, 18f, 46f, 54f);

            RectTransform homeColumn = body.Find("HomeSubstitutionsColumn") as RectTransform;
            RectTransform awayColumn = body.Find("AwaySubstitutionsColumn") as RectTransform;
            ConfigureDesignerTeamColumn(homeColumn, true);
            ConfigureDesignerTeamColumn(awayColumn, false);

            RectTransform footer = root.Find("Footer") as RectTransform;
            DestroyLayoutDriver(footer.gameObject);
            SetStretchBottom(footer, 18f, 18f, 38f);
            PositionFooterButton(view.confirmButton.GetComponent<RectTransform>(), -8f, 170f);
            PositionFooterButton(view.backButton.GetComponent<RectTransform>(), -188f, 130f);
            RectTransform footerSpacer = footer.Find("FooterSpacer") as RectTransform;
            if (footerSpacer != null)
            {
                SetStretch(footerSpacer, 0f, 320f, 0f, 0f);
            }
        }

        private static void ConfigureDesignerTeamColumn(RectTransform column, bool isHomeTeam)
        {
            DestroyLayoutDriver(column.gameObject);
            if (isHomeTeam)
            {
                SetAnchors(column, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(-6f, 0f));
            }
            else
            {
                SetAnchors(column, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(6f, 0f), new Vector2(0f, 0f));
            }

            RectTransform title = column.Find("TeamTitle") as RectTransform;
            SetStretchTop(title, 12f, 12f, 28f);

            RectTransform rosterArea = column.Find("RosterArea") as RectTransform;
            DestroyLayoutDriver(rosterArea.gameObject);
            SetStretchTop(rosterArea, 12f, 12f, 168f, 38f);

            RectTransform playingList = rosterArea.Find("PlayingList") as RectTransform;
            RectTransform benchList = rosterArea.Find("BenchList") as RectTransform;
            ConfigureRosterList(playingList, true);
            ConfigureRosterList(benchList, false);

            RectTransform remaining = column.Find("SubsRemaining") as RectTransform;
            SetStretchTop(remaining, 12f, 12f, 24f, 212f);

            RectTransform rows = column.Find("SubstitutionRows") as RectTransform;
            DestroyLayoutDriver(rows.gameObject);
            SetStretchBottom(rows, 12f, 12f, 148f, 12f);
            ConfigureSubstitutionRows(rows);
        }

        private static void ConfigureRosterList(RectTransform list, bool isLeft)
        {
            DestroyLayoutDriver(list.gameObject);
            if (isLeft)
            {
                SetAnchors(list, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, 0f));
            }
            else
            {
                SetAnchors(list, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(6f, 0f), Vector2.zero);
            }

            RectTransform header = list.Find(list.name.StartsWith("Playing") ? "PlayingHeader" : "BenchHeader") as RectTransform;
            SetStretchTop(header, 0f, 0f, 18f);
            RectTransform content = list.Find("Content") as RectTransform;
            SetStretch(content, 0f, 0f, 0f, 20f);
        }

        private static void ConfigureSubstitutionRows(RectTransform rows)
        {
            for (int index = 0; index < rows.childCount; index++)
            {
                RectTransform row = rows.GetChild(index) as RectTransform;
                DestroyLayoutDriver(row.gameObject);
                SetStretchTop(row, 0f, 0f, 32f, index * 36f);

                RectTransform outgoing = row.Find("OutgoingDropdown") as RectTransform;
                RectTransform incoming = row.Find("IncomingDropdown") as RectTransform;
                SetAnchors(outgoing, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, 0f));
                SetAnchors(incoming, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(4f, 0f), Vector2.zero);
            }
        }

        private static void PositionFooterButton(RectTransform button, float rightOffset, float width)
        {
            button.anchorMin = new Vector2(1f, 0.5f);
            button.anchorMax = new Vector2(1f, 0.5f);
            button.pivot = new Vector2(1f, 0.5f);
            button.anchoredPosition = new Vector2(rightOffset, 0f);
            button.sizeDelta = new Vector2(width, 34f);
        }

        private static void DestroyLayoutDriver(GameObject gameObject)
        {
            LayoutGroup layoutGroup = gameObject.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                Object.DestroyImmediate(layoutGroup);
            }

            LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                Object.DestroyImmediate(layoutElement);
            }

            ContentSizeFitter contentSizeFitter = gameObject.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter != null)
            {
                Object.DestroyImmediate(contentSizeFitter);
            }
        }

        private static void BuildFooter(Transform parent, SubstitutionMenuView view)
        {
            GameObject footer = CreateRect("Footer", parent);
            footer.AddComponent<LayoutElement>().preferredHeight = 38f;
            HorizontalLayoutGroup footerGroup = footer.AddComponent<HorizontalLayoutGroup>();
            footerGroup.childAlignment = TextAnchor.MiddleRight;
            footerGroup.spacing = 10;
            footerGroup.childControlWidth = false;
            footerGroup.childControlHeight = false;
            footerGroup.childForceExpandWidth = false;
            footerGroup.childForceExpandHeight = false;

            GameObject spacer = CreateRect("FooterSpacer", footer.transform);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
            view.backButton = CreateButton(footer.transform, "BackButton", "Back", 130f, 34f);
            view.confirmButton = CreateButton(footer.transform, "ConfirmSubsButton", "Confirm Subs", 170f, 34f);
        }

        private static Transform CreateRosterListShell(Transform parent, string title)
        {
            GameObject listObject = CreateRect($"{title}List", parent);
            LayoutElement listLayout = listObject.AddComponent<LayoutElement>();
            listLayout.flexibleWidth = 1f;
            listLayout.minWidth = 0f;

            VerticalLayoutGroup listGroup = listObject.AddComponent<VerticalLayoutGroup>();
            listGroup.spacing = 2;
            listGroup.childControlWidth = true;
            listGroup.childControlHeight = false;
            listGroup.childForceExpandWidth = true;
            listGroup.childForceExpandHeight = false;

            TextMeshProUGUI header = CreateText($"{title}Header", listObject.transform, title, 14f, MutedTextColor, TextAlignmentOptions.Left);
            header.fontStyle = FontStyles.Bold;
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            GameObject content = CreateRect("Content", listObject.transform);
            VerticalLayoutGroup contentGroup = content.AddComponent<VerticalLayoutGroup>();
            contentGroup.spacing = 2;
            contentGroup.childControlWidth = true;
            contentGroup.childControlHeight = false;
            contentGroup.childForceExpandWidth = true;
            contentGroup.childForceExpandHeight = false;
            return content.transform;
        }

        private static void AddSampleRosterRows(Transform parent, int firstJersey, int lastJersey)
        {
            for (int jersey = firstJersey; jersey <= lastJersey; jersey++)
            {
                TextMeshProUGUI row = CreateText($"Player_{jersey}", parent, $"{jersey}. Player Name", 13f, TextColor, TextAlignmentOptions.Left);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 17f;
            }
        }

        private static SubstitutionDropdownRowView CreateDropdownRow(Transform parent, bool isHomeTeam, int index)
        {
            GameObject rowObject = CreateRect($"{(isHomeTeam ? "Home" : "Away")}SubRow_{index + 1}", parent);
            rowObject.AddComponent<LayoutElement>().preferredHeight = 32f;
            HorizontalLayoutGroup rowLayout = rowObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            SubstitutionDropdownRowView rowView = rowObject.AddComponent<SubstitutionDropdownRowView>();
            rowView.outgoingDropdown = CreateDropdown(rowObject.transform, "OutgoingDropdown", "v", OutDropdownColor);
            rowView.incomingDropdown = CreateDropdown(rowObject.transform, "IncomingDropdown", "^", InDropdownColor);
            return rowView;
        }

        private static TMP_Dropdown CreateDropdown(Transform parent, string name, string arrowText, Color backgroundColor)
        {
            GameObject root = CreateRect(name, parent, typeof(Image), typeof(TMP_Dropdown));
            root.GetComponent<Image>().color = backgroundColor;
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.minHeight = 30f;
            layout.preferredHeight = 30f;

            TMP_Dropdown dropdown = root.GetComponent<TMP_Dropdown>();
            TextMeshProUGUI label = CreateText("Label", root.transform, "-", 13f, TextColor, TextAlignmentOptions.MidlineLeft);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = new Vector2(-34f, 0f);

            TextMeshProUGUI arrow = CreateText("Arrow", root.transform, arrowText, 16f, TextColor, TextAlignmentOptions.Center);
            RectTransform arrowRect = arrow.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1f, 0f);
            arrowRect.anchorMax = new Vector2(1f, 1f);
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.sizeDelta = new Vector2(28f, 0f);
            arrowRect.anchoredPosition = Vector2.zero;

            RectTransform template = CreateDropdownTemplate(root.transform, out TextMeshProUGUI itemText);
            dropdown.template = template;
            dropdown.captionText = label;
            dropdown.itemText = itemText;
            dropdown.ClearOptions();
            dropdown.AddOptions(new System.Collections.Generic.List<TMP_Dropdown.OptionData> { new("-") });
            dropdown.RefreshShownValue();
            return dropdown;
        }

        private static RectTransform CreateDropdownTemplate(Transform parent, out TextMeshProUGUI itemText)
        {
            GameObject template = CreateRect("Template", parent, typeof(Image), typeof(ScrollRect));
            template.SetActive(false);
            template.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.1f, 0.98f);
            RectTransform templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 1f);
            templateRect.anchorMax = new Vector2(1f, 1f);
            templateRect.pivot = new Vector2(0.5f, 0f);
            templateRect.anchoredPosition = Vector2.zero;
            templateRect.sizeDelta = new Vector2(0f, 132f);

            GameObject viewport = CreateRect("Viewport", template.transform, typeof(Image), typeof(Mask));
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            Stretch(viewport.GetComponent<RectTransform>());

            GameObject content = CreateRect("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 28f);
            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject item = CreateRect("Item", content.transform, typeof(Toggle), typeof(Image));
            item.AddComponent<LayoutElement>().preferredHeight = 26f;
            Toggle toggle = item.GetComponent<Toggle>();
            Image itemImage = item.GetComponent<Image>();
            itemImage.color = new Color(0.16f, 0.17f, 0.19f, 1f);
            toggle.targetGraphic = itemImage;

            itemText = CreateText("Item Label", item.transform, "Option", 13f, TextColor, TextAlignmentOptions.MidlineLeft);
            RectTransform itemTextRect = itemText.GetComponent<RectTransform>();
            itemTextRect.anchorMin = Vector2.zero;
            itemTextRect.anchorMax = Vector2.one;
            itemTextRect.offsetMin = new Vector2(10f, 0f);
            itemTextRect.offsetMax = new Vector2(-10f, 0f);

            ScrollRect scrollRect = template.GetComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            return templateRect;
        }

        private static Button CreateButton(Transform parent, string name, string label, float width, float height)
        {
            GameObject buttonObject = CreateRect(name, parent, typeof(Image), typeof(Button));
            buttonObject.GetComponent<Image>().color = ButtonColor;
            Button button = buttonObject.GetComponent<Button>();
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;

            TextMeshProUGUI text = CreateText("Text", button.transform, label, 18f, Color.black, TextAlignmentOptions.Center);
            Stretch(text.GetComponent<RectTransform>());
            return button;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            GameObject gameObject = CreateRect(name, parent, typeof(TextMeshProUGUI));
            TextMeshProUGUI label = gameObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Truncate;
            return label;
        }

        private static GameObject CreateRect(string name, Transform parent, params System.Type[] components)
        {
            System.Collections.Generic.List<System.Type> componentList = new() { typeof(RectTransform), typeof(CanvasRenderer) };
            componentList.AddRange(components);
            GameObject gameObject = new(name, componentList.ToArray());
            gameObject.layer = LayerMask.NameToLayer("UI");
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }
            return gameObject;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetStretch(RectTransform rectTransform, float left, float right, float bottom, float top)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static void SetStretchTop(RectTransform rectTransform, float left, float right, float height, float top = 0f)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.offsetMin = new Vector2(left, -top - height);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static void SetStretchBottom(RectTransform rectTransform, float left, float right, float height, float bottom = 0f)
        {
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, bottom + height);
        }

        private static void SetAnchors(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string[] parts = assetPath.Split('/');
            string currentPath = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = $"{currentPath}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }
                currentPath = nextPath;
            }
        }
    }
}
#endif
