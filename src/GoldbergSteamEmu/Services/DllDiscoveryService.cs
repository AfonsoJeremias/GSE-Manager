using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;

namespace GoldbergSteamEmu.Services
{
    public static class DllDiscoveryService
    {
        private static readonly string[] DllNames = { "steam_api.dll", "steam_api64.dll", "steamclient.dll", "steamclient64.dll" };

        public static List<DllInfo> Discover(string installDir)
        {
            var results = new List<DllInfo>();
            if (!Directory.Exists(installDir)) return results;
            foreach (var path in Directory.EnumerateFiles(installDir, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(path).ToLowerInvariant();
                if (Array.IndexOf(DllNames, name) < 0) continue;
                results.Add(new DllInfo
                {
                    FullPath = path,
                    OriginalPath = GetRelative(installDir, path),
                    Arch = GetArchitecture(path)
                });
            }
            return results;
        }

        public static string GetArchitecture(string dllPath)
        {
            try
            {
                using (var fs = File.OpenRead(dllPath))
                using (var pe = new PEReader(fs))
                {
                    var h = pe.PEHeaders;
                    return h.CoffHeader.Machine == Machine.Amd64 ? "x64"
                         : h.CoffHeader.Machine == Machine.I386 ? "x86"
                         : "unknown";
                }
            }
            catch { return "unknown"; }
        }

        private static string GetRelative(string root, string path)
        {
            return path.Substring(root.Length).TrimStart('\\', '/');
        }
    }
}