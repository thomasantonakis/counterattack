using System;
using UnityEngine;

/// <summary>Binds only the authored audio controls. Does not create or restyle UI.</summary>
public sealed class GameplayAudioSettingsView : MonoBehaviour
{
    [Serializable]
    public sealed class Row
    {
        public GameplayAudioCategory category;
        public UnityEngine.UI.Toggle enabled;
    }

    public Row[] rows = Array.Empty<Row>();
    private UnityEngine.Events.UnityAction<bool>[] enabledActions;

    private void OnEnable()
    {
        enabledActions = new UnityEngine.Events.UnityAction<bool>[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            Row row = rows[i];
            enabledActions[i] = value => GameplayAudioPreferences.Local.SetEnabled(row.category, value);
            if (row.enabled != null) row.enabled.onValueChanged.AddListener(enabledActions[i]);
        }
        GameplayAudioPreferences.Local.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameplayAudioPreferences.Local.Changed -= Refresh;
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i].enabled != null && enabledActions != null) rows[i].enabled.onValueChanged.RemoveListener(enabledActions[i]);
        }
    }

    public void Refresh()
    {
        foreach (Row row in rows)
        {
            if (row.enabled != null) row.enabled.SetIsOnWithoutNotify(GameplayAudioPreferences.Local.Enabled(row.category));
        }
    }
}
