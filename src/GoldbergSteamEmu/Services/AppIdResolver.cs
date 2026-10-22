using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GoldbergSteamEmu.Services
{
    public static class AppIdResolver
    {
        public static async Task<AppIdResolutionResult> ResolveAsync(string installDir, string gameName, string playniteGameId)
        {
            var r = new AppIdResolutionResult { GameName = gameName };

            if (!string.IsNullOrEmpty(playniteGameId) && Regex.IsMatch(playniteGameId, @"^\d+$"))
            {
                r.AppId = playniteGameId; r.Source = "Playnite GameId"; r.Confident = true; return r;
            }

            foreach (var candidate in new[] { "steam_appid.txt", "steam_settings/steam_appid.txt" })
            {
                var p = Path.Combine(installDir, candidate);
                if (File.Exists(p))
                {
                    var txt = File.ReadAllText(p).Trim();
                    if (Regex.IsMatch(txt, @"^\d+$")) { r.AppId = txt; r.Source = candidate; r.Confident = true; return r; }
                }
            }

            foreach (var ini in new[] { "steam_emu.ini", "ColdClientLoader.ini" })
            {
                var p = Path.Combine(installDir, ini);
                if (!File.Exists(p)) continue;
                foreach (var line in File.ReadAllLines(p))
                {
                    var m = Regex.Match(line, @"^\s*AppId\s*=\s*(\d+)", RegexOptions.IgnoreCase);
                    if (m.Success) { r.AppId = m.Groups[1].Value; r.Source = ini; r.Confident = true; return r; }
                }
            }

            // Fallback: Google search por nome — devolve não-confiante, UI pergunta ao utilizador
            r.Source = "none";
            r.Confident = false;
            return await Task.FromResult(r);
        }
    }
}