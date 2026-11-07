using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

// Fingerprint only the three authored resolution-panel hierarchies. Unrelated
// Room layouts, token positions and manager serialization are not fixture inputs.
public static class FrameworkAuthoredPanelSourceFingerprint
{
    private static readonly string[] Scripts =
    {
        "Assets/Scripts/UI/DuelResolutionPanelController.cs.meta",
        "Assets/Scripts/UI/HeaderChallengePanelController.cs.meta",
        "Assets/Scripts/UI/StoppageActionsPanelController.cs.meta"
    };
    private static string cachedSource;
    private static string cachedPanels;

    public static string Compute(string scenePath)
    {
        string scene = File.ReadAllText(scenePath).Replace("\r\n", "\n");
        string source = Digest(scene);
        if (source == cachedSource) return cachedPanels;
        var boundaries = Regex.Matches(scene, @"^--- !u!\d+ &(-?\d+)(?: stripped)?\n", RegexOptions.Multiline);
        var documents = new Dictionary<string, string>();
        for (int i = 0; i < boundaries.Count; i++)
        {
            int end = i + 1 < boundaries.Count ? boundaries[i + 1].Index : scene.Length;
            documents.Add(boundaries[i].Groups[1].Value, scene.Substring(boundaries[i].Index, end - boundaries[i].Index));
        }
        var pending = new Stack<string>();
        foreach (string script in Scripts)
        {
            string guid = Regex.Match(File.ReadAllText(script), @"guid: (\w+)").Groups[1].Value;
            string controller = documents.Values.Single(d => d.Contains("m_Script:") && d.Contains("guid: " + guid));
            pending.Push(Reference(controller, "m_GameObject"));
        }
        var owned = new HashSet<string>();
        while (pending.Count > 0)
        {
            string id = pending.Pop();
            if (id == "0" || !owned.Add(id)) continue;
            if (!documents.TryGetValue(id, out string document)) throw new InvalidOperationException($"Missing authored-panel document {id}.");
            if (document.Contains("GameObject:\n"))
                foreach (Match component in Regex.Matches(document, @"component: \{fileID: (-?\d+)\}")) pending.Push(component.Groups[1].Value);
            if ((document.Contains("Transform:\n") || document.Contains("RectTransform:\n")) && document.Contains("m_Children:"))
            {
                string children = document.Split(new[] { "m_Children:" }, StringSplitOptions.None)[1]
                    .Split(new[] { "m_Father:" }, StringSplitOptions.None)[0];
                foreach (Match child in Regex.Matches(children, @"fileID: (-?\d+)"))
                {
                    string transform = child.Groups[1].Value;
                    pending.Push(transform);
                    if (documents.TryGetValue(transform, out string childDocument)) pending.Push(Reference(childDocument, "m_GameObject"));
                }
            }
            string prefab = Reference(document, "m_PrefabInstance", required: false);
            if (prefab != null && prefab != "0") pending.Push(prefab);
        }
        string fingerprint = Digest(string.Concat(owned.OrderBy(long.Parse).Select(id => documents[id])));
        cachedSource = source;
        cachedPanels = fingerprint;
        return fingerprint;
    }

    private static string Reference(string document, string property, bool required = true)
    {
        var match = Regex.Match(document, Regex.Escape(property) + @": \{fileID: (-?\d+)\}");
        if (match.Success) return match.Groups[1].Value;
        if (required) throw new InvalidOperationException($"Missing {property} in an authored-panel document.");
        return null;
    }
    private static string Digest(string value)
    {
        using var hash = SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
    }
}
