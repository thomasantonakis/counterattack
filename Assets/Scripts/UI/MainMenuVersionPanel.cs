using CounterAttack.Online;
using TMPro;
using UnityEngine;

/// <summary>Shows the build and exact online handshake compatibility values on the main menu.</summary>
public sealed class MainMenuVersionPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private TextMeshProUGUI compatibilityText;

    private void Awake()
    {
        if (versionText != null)
            versionText.text = "Game " + Application.version;

        if (compatibilityText == null)
            return;

        try
        {
            OnlineCompatibility compatibility = OnlineFrontendSessionPolicy.Compatibility();
            compatibilityText.text = "Protocol " + compatibility.Protocol
                + "  •  Rules " + compatibility.Rules
                + "  •  Catalog " + ShortCatalog(compatibility.Catalog);
        }
        catch (System.Exception exception)
        {
            compatibilityText.text = "Online compatibility unavailable";
            Debug.LogError("Could not display online compatibility versions: " + exception.Message, this);
        }
    }

    private static string ShortCatalog(string catalog)
    {
        const string prefix = "catalog-";
        return catalog != null && catalog.StartsWith(prefix, System.StringComparison.Ordinal)
            ? catalog.Substring(prefix.Length, 8)
            : catalog;
    }
}
