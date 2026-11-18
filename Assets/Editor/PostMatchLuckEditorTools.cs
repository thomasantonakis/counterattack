using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class PostMatchLuckEditorTools
{
    public static void Author(GameObject root)
    {
        var content = root.transform.Find(PostMatchReportEditorTools.ContentPath);
        var font = content.Find("StatsGrid").GetComponentsInChildren<TMP_Text>(true).First(t => t.font != null).font;
        var tabs = content.Find("ReportTabs");
        var button = tabs.Find("LuckTab")?.GetComponent<Button>();
        if (button == null) button = PostMatchReportEditorTools.Button("LuckTab", tabs, "LUCK", font);
        var page = content.Find("Luck");
        if (page == null)
        {
            page = PostMatchReportEditorTools.Group("Luck", content, false).transform;
            page.GetComponent<RectTransform>().pivot = new Vector2(.5f, 1);
            page.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var title = Text("LuckHeading", page, "Luck compared with expectation", font, 28, 42);
            title.fontStyle = FontStyles.Bold;
            Text("LuckDescription", page, "Positive means more favourable outcomes than the recorded chances predicted. The two teams' scores mirror each other.", font, 21, 54);
            var names = PostMatchReportEditorTools.Group("Teams", page, true, 38).transform;
            Text("Spacer", names, "", font, 21, 38);
            var home = Text("LuckHomeName", names, "Home", font, 23, 38);
            var away = Text("LuckAwayName", names, "Away", font, 23, 38);
            home.color = new Color(.12f, .39f, .55f); away.color = new Color(.65f, .29f, .10f);
            var scores = PostMatchReportEditorTools.Group("Scores", page, true, 64).transform;
            Text("Label", scores, "Luck", font, 23, 64);
            var homeScore = Text("HomeLuck", scores, "No assessed checks", font, 30, 64);
            var awayScore = Text("AwayLuck", scores, "No assessed checks", font, 30, 64);
            var details = PostMatchReportEditorTools.Group("Details", page, true, 100).transform;
            Text("Label", details, "Compared with chance", font, 21, 100);
            var homeDetail = Text("HomeLuckDetail", details, "", font, 21, 100);
            var awayDetail = Text("AwayLuckDetail", details, "", font, 21, 100);
            var sample = Text("LuckCoverage", page, "No recorded checks.", font, 21, 58);
            var categoryLabels = new TMP_Text[6]; var homeValues = new TMP_Text[6]; var awayValues = new TMP_Text[6];
            for (int i = 0; i < 6; i++)
            {
                var row = PostMatchReportEditorTools.Group("LuckCategory" + i, page, true, 40).transform;
                categoryLabels[i] = Text("Category", row, PostMatchLuckData.Categories[i], font, 21, 40);
                homeValues[i] = Text("Home", row, "—", font, 21, 40);
                awayValues[i] = Text("Away", row, "—", font, 21, 40);
            }
            Text("LuckMethod", page, "Each resolved check has equal weight. Luck is actual minus expected, in percentage points (pp). Ties count as half; paired contests count once. Forced rolls, scatter/stoppage rolls and incomplete or unsupported records are excluded. This measures random outcomes, not manager skill or player ratings.", font, 20, 120);
            page.gameObject.AddComponent<PostMatchLuckView>().Configure(home, away, homeScore, awayScore, homeDetail, awayDetail, sample,
                categoryLabels, homeValues, awayValues);
            page.gameObject.SetActive(false);
        }
        // Explicit editor authoring: keep this new page readable in both existing panel themes.
        var background = page.GetComponent<UnityEngine.UI.Image>();
        if (background == null) background = page.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(.97f, .98f, .99f); background.raycastTarget = false;
        var layoutGroup = page.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layoutGroup.spacing = 4; layoutGroup.padding = new RectOffset(8, 8, 8, 8);
        foreach (var text in page.GetComponentsInChildren<TMP_Text>(true))
        {
            var layout = text.GetComponent<UnityEngine.UI.LayoutElement>();
            layout.minWidth = 0; layout.preferredWidth = 0; layout.flexibleWidth = 1;
        }
        page.Find("LuckHeading").GetComponent<LayoutElement>().preferredHeight = 36;
        page.Find("LuckDescription").GetComponent<LayoutElement>().preferredHeight = 44;
        page.Find("Teams").GetComponent<LayoutElement>().preferredHeight = 30;
        page.Find("Scores").GetComponent<LayoutElement>().preferredHeight = 46;
        page.Find("Details").GetComponent<LayoutElement>().preferredHeight = 80;
        page.Find("LuckCoverage").GetComponent<LayoutElement>().preferredHeight = 48;
        for (int i = 0; i < 6; i++) page.Find("LuckCategory" + i).GetComponent<LayoutElement>().preferredHeight = 34;
        page.Find("LuckMethod").GetComponent<LayoutElement>().preferredHeight = 110;
        root.GetComponent<PostMatchReportController>().ConfigureLuck(button, page.GetComponent<PostMatchLuckView>());
    }
    private static TMP_Text Text(string name, Transform parent, string value, TMP_FontAsset font, int size, float height)
    {
        var text = PostMatchReportEditorTools.Text(name, parent, value, font, size, height);
        text.color = new Color(.12f, .26f, .33f);
        text.textWrappingMode = TextWrappingModes.Normal;
        var layout = text.GetComponent<LayoutElement>(); layout.minWidth = 0; layout.preferredWidth = 0; layout.flexibleWidth = 1;
        return text;
    }
}
