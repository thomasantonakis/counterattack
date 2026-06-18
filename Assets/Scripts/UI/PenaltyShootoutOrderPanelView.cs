using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PenaltyShootoutOrderPanelView : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text homeHeaderText;
    public TMP_Text awayHeaderText;
    public Transform homeRowsContainer;
    public Transform awayRowsContainer;
    public PenaltyShootoutOrderRowView[] homeRows;
    public PenaltyShootoutOrderRowView[] awayRows;
    public PenaltyShootoutOrderRowView rowTemplate;
    public Button startButton;

    public PenaltyShootoutOrderRowView[] GetRows(bool isHomeTeam)
    {
        PenaltyShootoutOrderRowView[] configuredRows = isHomeTeam ? homeRows : awayRows;
        if (configuredRows != null && configuredRows.Length > 0)
        {
            return configuredRows;
        }

        Transform container = isHomeTeam ? homeRowsContainer : awayRowsContainer;
        return container != null
            ? container.GetComponentsInChildren<PenaltyShootoutOrderRowView>(true)
            : new PenaltyShootoutOrderRowView[0];
    }

    public bool HasRequiredReferences()
    {
        return titleText != null
            && homeHeaderText != null
            && awayHeaderText != null
            && homeRowsContainer != null
            && awayRowsContainer != null
            && startButton != null
            && HasUsableRows(GetRows(true), 11)
            && HasUsableRows(GetRows(false), 11);
    }

    private static bool HasUsableRows(PenaltyShootoutOrderRowView[] rows, int minimumCount)
    {
        if (rows == null || rows.Length < minimumCount)
        {
            return false;
        }

        for (int i = 0; i < minimumCount; i++)
        {
            if (rows[i] == null || rows[i].nameText == null || rows[i].shootingText == null)
            {
                return false;
            }
        }

        return true;
    }
}
