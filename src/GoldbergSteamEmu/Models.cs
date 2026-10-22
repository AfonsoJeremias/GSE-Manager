using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GoldbergSteamEmu
{
    public enum EmulatorState { Original = 0, Deployed = 1, Dirty = 2, Unknown = 3 }

    public enum BackupMethod { Bak, Subfolder }

    public class DllInfo
    {
        public string OriginalPath { get; set; }     // relative to InstallDir
        public string FullPath { get; set; }
        public string BackupMethod { get; set; }     // "bak" | "subfolder"
        public string BackupPath { get; set; }
        public string Arch { get; set; }             // "x86" | "x64"
        public string Sha256Original { get; set; }
        public string Sha256Applied { get; set; }
        public string DetectedCrack { get; set; }
        public List<string> PreservedCrackBackups { get; set; } = new List<string>();
    }

    public class GoldbergManifest
    {
        public int Schema { get; set; } = 1;
        public string AppId { get; set; }
        public int State { get; set; }
        public string AppliedAt { get; set; }
        public string EmuVersion { get; set; } = "release";
        public string ToolVersion { get; set; } = "1.0";
        public List<DllInfo> Dlls { get; set; } = new List<DllInfo>();
        public List<string> CreatedPaths { get; set; } = new List<string>();

        public static GoldbergManifest Load(string path)
        {
            if (!System.IO.File.Exists(path)) return null;
            return JsonConvert.DeserializeObject<GoldbergManifest>(System.IO.File.ReadAllText(path));
        }

        public void Save(string path)
        {
            System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }

    public class AppIdResolutionResult
    {
        public string AppId { get; set; }
        public string GameName { get; set; }
        public string Source { get; set; }
        public bool Confident { get; set; }
        public List<AppIdCandidate> Candidates { get; set; } = new List<AppIdCandidate>();
    }

    public class AppIdCandidate
    {
        public string AppId { get; set; }
        public string Name { get; set; }
        public string ThumbnailUrl { get; set; }
    }

    public class DeployOptions
    {
        public bool IncludeAchievements { get; set; } = true;
        public bool IncludeImages { get; set; } = true;
        public bool GenerateInterfaces { get; set; } = true;
        public bool DeployAchievementWatcher { get; set; } = false;
        public bool UseAnonymousAccount { get; set; } = true;
        public string Username { get; set; }
        public string Password { get; set; }
        public string EmuVersion { get; set; } = "release";
        public Dictionary<string, Dictionary<string, string>> ConfigOverrides { get; set; }
            = new Dictionary<string, Dictionary<string, string>>();
    }

    public class PipelineResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public string AppId { get; set; }
        public List<string> Steps { get; set; } = new List<string>();
    }
}