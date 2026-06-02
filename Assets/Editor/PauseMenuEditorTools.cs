using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CounterAttack.Editor
{
    public static class PauseMenuEditorTools
    {
        [MenuItem("CounterAttack/Room/Ensure Pause Menu Substitutions Button")]
        public static void EnsureSubstitutionsButton()
        {
            PauseMenuManager manager = Object.FindAnyObjectByType<PauseMenuManager>();
            if (manager == null)
            {
                Debug.LogError("PauseMenuManager not found in the open scene.");
                return;
            }

            if (manager.pausePanel == null || manager.resumeButton == null)
            {
                Debug.LogError("PauseMenuManager must have pausePanel and resumeButton assigned before adding the Substitutions button.");
                return;
            }

            Button button = manager.substitutionsButton != null
                ? manager.substitutionsButton
                : FindExistingButton(manager.pausePanel.transform);

            if (button == null)
            {
                button = Object.Instantiate(manager.resumeButton, manager.resumeButton.transform.parent);
                button.name = "SubstitutionsButton";
                button.transform.SetSiblingIndex(manager.resumeButton.transform.GetSiblingIndex() + 1);
                RectTransform rect = button.transform as RectTransform;
                RectTransform resumeRect = manager.resumeButton.transform as RectTransform;
                if (rect != null && resumeRect != null)
                {
                    rect.anchoredPosition = resumeRect.anchoredPosition + new Vector2(0f, -60f);
                    rect.sizeDelta = resumeRect.sizeDelta;
                }
            }

            SetButtonText(button, "Substitutions");
            button.onClick.RemoveAllListeners();

            SerializedObject serializedManager = new(manager);
            serializedManager.FindProperty("substitutionsButton").objectReferenceValue = button;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(button);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            EditorSceneManager.SaveScene(manager.gameObject.scene);
            Debug.Log("Pause menu Substitutions button is present and assigned.");
        }

        private static Button FindExistingButton(Transform pausePanel)
        {
            foreach (Button button in pausePanel.GetComponentsInChildren<Button>(true))
            {
                if (button.name == "SubstitutionsButton")
                {
                    return button;
                }
            }

            return null;
        }

        private static void SetButtonText(Button button, string text)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = text;
            }
        }
    }
}
