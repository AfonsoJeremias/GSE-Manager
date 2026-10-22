using System;
using System.Collections.Generic;
using System.IO;

namespace GoldbergSteamEmu.Services
{
    public static class DllDiscoveryService
    {
        private static readonly string[] DllNames =
        {
            "steam_api.dll",
            "steam_api64.dll",
            "steamclient.dll",
            "steamclient64.dll"
        };

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

        /// <summary>
        /// Lê o header PE de forma manual para determinar a arquitetura.
        /// Funciona no .NET Framework 4.6.2 (onde PortableExecutable não existe).
        /// </summary>
        public static string GetArchitecture(string dllPath)
        {
            try
            {
                using (var fs = File.OpenRead(dllPath))
                using (var br = new BinaryReader(fs))
                {
                    if (fs.Length < 0x40) return "unknown";

                    // Offset do "PE\0\0" está em 0x3C
                    fs.Seek(0x3C, SeekOrigin.Begin);
                    int peOffset = br.ReadInt32();

                    if (peOffset < 0 || peOffset + 6 > fs.Length) return "unknown";

                    fs.Seek(peOffset, SeekOrigin.Begin);
                    uint peSignature = br.ReadUInt32();
                    if (peSignature != 0x00004550) return "unknown"; // "PE\0\0"

                    ushort machine = br.ReadUInt16();
                    switch (machine)
                    {
                        case 0x014c: return "x86";   // IMAGE_FILE_MACHINE_I386
                        case 0x8664: return "x64";   // IMAGE_FILE_MACHINE_AMD64
                        case 0xAA64: return "arm64"; // IMAGE_FILE_MACHINE_ARM64
                        default:     return "unknown";
                    }
                }
            }
            catch
            {
                return "unknown";
            }
        }

        private static string GetRelative(string root, string path)
        {
            return path.Substring(root.Length).TrimStart('\\', '/');
        }
    }
}
