using System;
using System.IO;

namespace GoldbergSteamEmu.Services
{
    public static class EmulatorDeployService
    {
        public static void DeployDllsForLocation(string installPath, string dllDir, string emuRoot)
        {
            CopyDirRecursive(Path.Combine(emuRoot, "release", "experimental", "x32"), dllDir);
            CopyDirRecursive(Path.Combine(emuRoot, "release", "experimental", "x64"), dllDir);
        }

        public static void CopySteamSettings(string canonSteamSettings, string dllDir)
        {
            CopyDirRecursive(canonSteamSettings, Path.Combine(dllDir, "steam_settings"));
        }

        public static void CopyDirRecursive(string src, string dst)
        {
            if (!Directory.Exists(src)) return;
            Directory.CreateDirectory(dst);
            foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
            {
                var rel = file.Substring(src.Length).TrimStart('\\', '/');
                var target = Path.Combine(dst, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target, true);
            }
        }

        public static string BuildCanonicalSteamSettings(string emuRoot, string outputAppIdDir, string destRoot, DeployOptions opts)
        {
            var temp = Path.Combine(destRoot, "_gse_canonical_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);

            // Base
            var baseX64 = Path.Combine(emuRoot, "release", "experimental", "x64", "steam_settings");
            if (Directory.Exists(baseX64)) CopyDirRecursive(baseX64, temp);

            // Gerado
            if (Directory.Exists(outputAppIdDir))
            {
                var genSS = Path.Combine(outputAppIdDir, "steam_settings");
                if (Directory.Exists(genSS)) CopyDirRecursive(genSS, temp);
            }

            // Configs base (fallback se não existirem em genSS)
            var cfgDir = Path.Combine(emuRoot, "configs");
            foreach (var cfg in new[] { "configs.app.ini", "configs.main.ini", "configs.overlay.ini", "configs.user.ini" })
            {
                var src = Path.Combine(cfgDir, cfg);
                var dst = Path.Combine(temp, cfg);
                if (File.Exists(src) && !File.Exists(dst)) File.Copy(src, dst);
            }

            // Overrides
            if (opts.ConfigOverrides != null)
            {
                foreach (var fileName in new[] { "configs.app.ini", "configs.main.ini", "configs.overlay.ini", "configs.user.ini" })
                {
                    var key = fileName;
                    if (opts.ConfigOverrides.TryGetValue(key, out var ov))
                        ConfigIniService.ApplyOverrides(Path.Combine(temp, fileName), ov);
                }
            }
            return temp;
        }
    }
}