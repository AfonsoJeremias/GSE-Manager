using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GoldbergSteamEmu.Services
{
    /// <summary>Editor de INI que preserva comentários e ordem. Só substitui chave=valor existentes.</summary>
    public static class ConfigIniService
    {
        public static void ApplyOverrides(string iniPath, Dictionary<string, string> overrides)
        {
            if (!File.Exists(iniPath) || overrides == null || overrides.Count == 0) return;
            var lines = File.ReadAllLines(iniPath);
            var replaced = new HashSet<string>();
            string currentSection = null;
            var regexCache = new Dictionary<string, Regex>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var secMatch = Regex.Match(line, @"^\s*\[(.+?)\]\s*$");
                if (secMatch.Success) { currentSection = secMatch.Groups[1].Value.Trim(); continue; }

                if (currentSection == null) continue;

                var keyMatch = Regex.Match(line, @"^\s*([A-Za-z0-9_:\.\-]+)\s*=");
                if (!keyMatch.Success) continue;

                var key = keyMatch.Groups[1].Value;
                var fullKey = currentSection + "::" + key;
                if (overrides.TryGetValue(fullKey, out var value))
                {
                    lines[i] = key + "=" + value;
                    replaced.Add(fullKey);
                }
            }

            // Append chaves novas em falta (preserva comentários originais)
            var additionsBySection = new Dictionary<string, List<string>>();
            foreach (var kv in overrides)
            {
                if (replaced.Contains(kv.Key)) continue;
                var parts = kv.Key.Split(new[] { "::" }, 2, StringSplitOptions.None);
                if (parts.Length != 2) continue;
                if (!additionsBySection.ContainsKey(parts[0])) additionsBySection[parts[0]] = new List<string>();
                additionsBySection[parts[0]].Add(parts[1] + "=" + kv.Value);
            }

            if (additionsBySection.Count == 0) { File.WriteAllLines(iniPath, lines); return; }

            var sb = new StringBuilder();
            foreach (var l in lines) sb.AppendLine(l);
            foreach (var kv in additionsBySection)
            {
                sb.AppendLine();
                sb.AppendLine("[" + kv.Key + "]");
                foreach (var add in kv.Value) sb.AppendLine(add);
            }
            File.WriteAllText(iniPath, sb.ToString());
        }
    }
}