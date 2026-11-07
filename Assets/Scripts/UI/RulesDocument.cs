using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>The deliberately small Markdown dialect used by the bundled rulebook.</summary>
public static class RulesDocument
{
    public sealed class Block
    {
        public string anchor;
        public string section;
        public string text;
        public string plain;
        public string image;
        public int level;
    }

    public readonly struct Match
    {
        public readonly int block;
        public readonly int start;
        public readonly int length;
        public Match(int block, int start, int length) { this.block = block; this.start = start; this.length = length; }
    }

    public static string Slug(string value) => Regex.Replace(
        Regex.Replace(Plain(value).ToLowerInvariant(), @"[^\p{L}\p{N}\s_-]", ""), @"\s+", "-");

    public static string Plain(string value)
    {
        value = Regex.Replace(value, @"!?\[([^\]]+)\]\([^)]+\)", "$1");
        return value.Replace("**", "").Replace("*", "").Replace("`", "").Replace("_", "");
    }

    public static string LinkTarget(string url, string page)
    {
        const string prefix = "https://counterattackgame.github.io/wiki/en/";
        if (url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) url = url.Substring(prefix.Length);
        else if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) return url;
        if (url.StartsWith("#")) return page + url;
        url = url.Replace(".md", "").Replace(".html", "");
        return url.Replace("../en/", "");
    }

    public static string Rich(string value, string page)
    {
        // Escape TMP markup from prose (the wiki uses < and >= in comparisons).
        value = Regex.Replace(value, "[<>]", m => "<noparse>" + m.Value + "</noparse>");
        value = Regex.Replace(value, @"\[([^\]]+)\]\(([^)]+)\)", m =>
            "<link=\"" + LinkTarget(m.Groups[2].Value, page) + "\"><color=#79CEF2><u>" + m.Groups[1].Value + "</u></color></link>");
        value = Regex.Replace(value, @"\*\*(.+?)\*\*", "<b>$1</b>");
        value = Regex.Replace(value, @"(?<!\*)\*([^*]+)\*(?!\*)", "<i>$1</i>");
        value = Regex.Replace(value, @"(?<!\w)_([^_]+)_(?!\w)", "<i>$1</i>");
        return value.Replace("`", "");
    }

    public static List<Block> Parse(string page, string markdown)
    {
        var blocks = new List<Block>();
        var slugs = new Dictionary<string, int>();
        string section = page;
        bool firstHeading = true;
        foreach (string source in markdown.Replace("\r", "").Split('\n'))
        {
            string line = source.Trim();
            if (line.Length == 0 || line.StartsWith("[Read this page") || line.StartsWith("[Back to Home Page]")) continue;
            var heading = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
            if (heading.Success)
            {
                string title = Plain(heading.Groups[2].Value);
                string slug = Slug(title);
                slugs.TryGetValue(slug, out int duplicate);
                slugs[slug] = duplicate + 1;
                string anchor = firstHeading ? page : page + "#" + slug + (duplicate > 0 ? "-" + duplicate : "");
                firstHeading = false;
                section = title;
                blocks.Add(new Block { anchor = anchor, section = title, text = title, plain = title, level = heading.Groups[1].Length });
                continue;
            }
            var image = Regex.Match(line, @"^!\[([^\]]*)\]\(([^)]+)\)$");
            if (image.Success)
            {
                blocks.Add(new Block { section = section, image = System.IO.Path.GetFileName(image.Groups[2].Value), plain = image.Groups[1].Value, text = image.Groups[1].Value });
                continue;
            }
            if (line.StartsWith("- ")) line = "• " + line.Substring(2);
            blocks.Add(new Block { section = section, text = Rich(line, page), plain = Plain(line) });
        }
        return blocks;
    }

    public static List<Match> Search(IReadOnlyList<string> texts, string query)
    {
        var matches = new List<Match>();
        query = query?.Trim();
        if (string.IsNullOrEmpty(query)) return matches;
        for (int block = 0; block < texts.Count; block++)
        {
            string text = texts[block] ?? "";
            for (int start = 0; start <= text.Length - query.Length;)
            {
                int index = text.IndexOf(query, start, StringComparison.OrdinalIgnoreCase);
                if (index < 0) break;
                matches.Add(new Match(block, index, query.Length));
                start = index + query.Length;
            }
        }
        return matches;
    }

    public static void ValidateLinks(IEnumerable<Block> blocks)
    {
        var list = blocks.ToList();
        var anchors = new HashSet<string>(list.Where(b => !string.IsNullOrEmpty(b.anchor)).Select(b => b.anchor));
        foreach (Block block in list)
            foreach (System.Text.RegularExpressions.Match link in Regex.Matches(block.text, "<link=\"([^\"]+)\">"))
            {
                string target = link.Groups[1].Value;
                if (!target.StartsWith("https://") && !target.StartsWith("http://") && !anchors.Contains(target))
                    throw new InvalidOperationException("Broken rulebook link: " + target);
            }
    }
}
