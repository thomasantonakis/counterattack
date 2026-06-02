using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SubstitutionMenuView : MonoBehaviour
{
    public TextMeshProUGUI titleText;

    public TextMeshProUGUI homeTeamTitleText;
    public TextMeshProUGUI awayTeamTitleText;
    public TextMeshProUGUI homeSubsRemainingText;
    public TextMeshProUGUI awaySubsRemainingText;

    public Transform homePlayingListContent;
    public Transform homeBenchListContent;
    public Transform awayPlayingListContent;
    public Transform awayBenchListContent;

    public Transform homeSubstitutionRowsContainer;
    public Transform awaySubstitutionRowsContainer;

    public SubstitutionDropdownRowView[] homeRows;
    public SubstitutionDropdownRowView[] awayRows;

    public Button backButton;
    public Button confirmButton;

    public SubstitutionDropdownRowView[] GetRows(bool isHomeTeam)
    {
        SubstitutionDropdownRowView[] configuredRows = isHomeTeam ? homeRows : awayRows;
        if (configuredRows != null && configuredRows.Length > 0)
        {
            return configuredRows;
        }

        Transform container = isHomeTeam ? homeSubstitutionRowsContainer : awaySubstitutionRowsContainer;
        return container != null
            ? container.GetComponentsInChildren<SubstitutionDropdownRowView>(true)
            : new SubstitutionDropdownRowView[0];
    }

    public bool HasRequiredReferences()
    {
        SubstitutionDropdownRowView[] resolvedHomeRows = GetRows(true);
        SubstitutionDropdownRowView[] resolvedAwayRows = GetRows(false);
        return homeTeamTitleText != null
            && awayTeamTitleText != null
            && homeSubsRemainingText != null
            && awaySubsRemainingText != null
            && homePlayingListContent != null
            && homeBenchListContent != null
            && awayPlayingListContent != null
            && awayBenchListContent != null
            && homeSubstitutionRowsContainer != null
            && awaySubstitutionRowsContainer != null
            && backButton != null
            && confirmButton != null
            && HasUsableRows(resolvedHomeRows)
            && HasUsableRows(resolvedAwayRows);
    }

    private static bool HasUsableRows(SubstitutionDropdownRowView[] rows)
    {
        if (rows == null || rows.Length == 0)
        {
            return false;
        }

        foreach (SubstitutionDropdownRowView row in rows)
        {
            if (row == null || row.outgoingDropdown == null || row.incomingDropdown == null)
            {
                return false;
            }
        }

        return true;
    }
}
