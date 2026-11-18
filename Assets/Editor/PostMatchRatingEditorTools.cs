using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class PostMatchRatingEditorTools
{
    public static void Author(GameObject root)
    {
        var content = root.transform.Find(PostMatchReportEditorTools.ContentPath);
        var font = content.Find("StatsGrid").GetComponentsInChildren<TMP_Text>(true).First(t => t.font != null).font;
        var tab = content.Find("ReportTabs/RatingsTab")?.GetComponent<Button>();
        if (tab == null) tab = PostMatchReportEditorTools.Button("RatingsTab", content.Find("ReportTabs"), "RATINGS", font);
        var page = content.Find("Ratings");
        if (page == null)
        {
            page = PostMatchReportEditorTools.Group("Ratings", content, false).transform;
            page.GetComponent<RectTransform>().pivot = new Vector2(.5f, 1);
            var background = page.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(.97f,.98f,.99f); background.raycastTarget = false;
            page.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = 4;
            page.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().padding = new RectOffset(8,8,8,8);
            page.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Text("RatingHeading", page, "Player ratings · PROTOTYPE", font, 27, 38);
            Text("RatingLimitations", page, "One formula for all positions. No minutes/role adjustment. Click a player to inspect the calculation; NR means no weighted contributions recorded.", font, 20, 58);
            var headings = PostMatchReportEditorTools.Group("RatingColumns", page, true, 30).transform;
            Cell("Rank", headings, "#", font, 60, false); Cell("Player", headings, "PLAYER", font, 4, true);
            Cell("Team", headings, "TEAM", font, 3, true); Cell("Rating", headings, "RATING", font, 1, true);
            var rows = new PostMatchRatingView.PlayerRow[8];
            for(int i=0;i<rows.Length;i++)
            {
                var row = PostMatchReportEditorTools.Group("RatingRow" + i, page, true, 34);
                row.AddComponent<Image>().color = Color.white;
                var button = row.AddComponent<Button>(); button.targetGraphic = row.GetComponent<Image>();
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                rows[i] = new PostMatchRatingView.PlayerRow { button = button,
                    rank = Cell("Rank", row.transform, "", font, 60, false), player = Cell("Player", row.transform, "", font, 4, true),
                    team = Cell("Team", row.transform, "", font, 3, true), rating = Cell("Rating", row.transform, "", font, 1, true) };
            }
            var paging = PostMatchReportEditorTools.Group("RatingPaging", page, true, 40).transform;
            var previous = PostMatchReportEditorTools.Button("RatingPrevious", paging, "Previous", font);
            var label = Text("RatingPage", paging, "", font, 20, 40);
            var next = PostMatchReportEditorTools.Button("RatingNext", paging, "Next", font);
            var detail = Text("RatingBreakdown", page, "Select a player", font, 20, 100);
            Text("RatingFormula", page, PostMatchRatingData.FormulaText, font, 19, 82);
            Text("RatingMethod", page, "Prototype weights, not a calibrated performance model. Distance and injuries do not affect the score. Recoveries are counted once; tackles, interceptions and duel-win aliases receive no extra credit. Cards are cumulative. Luck is reported separately.", font, 18, 70);
            page.gameObject.AddComponent<PostMatchRatingView>().Configure(rows, label, detail, previous, next);
            page.gameObject.SetActive(false);
        }
        page.Find("RatingBreakdown").GetComponent<LayoutElement>().preferredHeight = 100;
        page.Find("RatingFormula").GetComponent<LayoutElement>().preferredHeight = 82;
        page.Find("RatingMethod").GetComponent<LayoutElement>().preferredHeight = 70;
        root.GetComponent<PostMatchReportController>().ConfigureRatings(tab, page.GetComponent<PostMatchRatingView>());
    }
    private static TMP_Text Text(string name, Transform parent, string value, TMP_FontAsset font, int size, float height)
    {
        var text = PostMatchReportEditorTools.Text(name,parent,value,font,size,height);
        text.color = new Color(.12f,.26f,.33f); text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }
    private static TMP_Text Cell(string name, Transform parent, string value, TMP_FontAsset font, float width, bool flexible)
    {
        var text = Text(name,parent,value,font,20,32); var layout=text.GetComponent<LayoutElement>();
        text.textWrappingMode = TextWrappingModes.NoWrap;
        layout.minWidth = flexible ? 0 : width; layout.preferredWidth = flexible ? 0 : width; layout.flexibleWidth = flexible ? width : 0;
        return text;
    }
}
