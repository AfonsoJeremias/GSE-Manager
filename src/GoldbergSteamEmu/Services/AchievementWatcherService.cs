using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace GoldbergSteamEmu.Services
{
    public static class AchievementWatcherService
    {
        public static void Deploy(string emuRoot, string savesDir, string appDataPath)
        {
            var zip = Path.Combine(emuRoot, "extra_acw.zip");
            var awDir = Path.Combine(appDataPath, "Achievement Watcher");
            if (File.Exists(zip))
            {
                var sevenZip = Path.Combine(emuRoot, "tools", "7za", "7za.exe");
                if (File.Exists(sevenZip))
                    ProcessRunner.Run(sevenZip, $"x \"{zip}\" -o\"{awDir}\" -aoa", emuRoot);
            }

            var cfgDir = Path.Combine(awDir, "cfg");
            Directory.CreateDirectory(cfgDir);
            var userdir = Path.Combine(cfgDir, "userdir.db");
            if (!File.Exists(userdir)) File.WriteAllText(userdir, "[\n]\n");

            var existing = File.ReadAllText(userdir);
            var escaped = savesDir.Replace("\\", "\\\\");
            if (existing.Contains(escaped)) return;

            var entry = "  {\n    \"path\": \"" + escaped + "\",\n    \"notify\": true\n  }";
            var trimmed = existing.TrimEnd().TrimEnd(']');
            if (trimmed.EndsWith("{")) trimmed += ",\n";
            else if (!trimmed.TrimEnd().EndsWith("[")) trimmed += ",\n";
            File.WriteAllText(userdir, trimmed + entry + "\n]\n");
        }
    }
}