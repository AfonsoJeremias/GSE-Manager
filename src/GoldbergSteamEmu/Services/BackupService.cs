using System;
using System.IO;

namespace GoldbergSteamEmu.Services
{
    /// <summary>Decisão sobre onde guardar o backup de uma DLL.</summary>
    public class BackupDecision
    {
        public BackupMethod Method { get; set; }
        public string Path { get; set; }
        public string DetectedCrack { get; set; }
    }

    public static class BackupService
    {
        public static BackupDecision DecideBackup(DllInfo dll)
        {
            var dir = Path.GetDirectoryName(dll.FullPath);
            var bakPath = dll.FullPath + ".bak";

            string crack = null;
            bool crackSignals = false;

            if (File.Exists(bakPath)) { crackSignals = true; crack = "existing .bak"; }
            else if (File.Exists(dll.FullPath + ".org")) { crackSignals = true; crack = ".org"; }
            else if (File.Exists(dll.FullPath + ".orig")) { crackSignals = true; crack = ".orig"; }
            else if (File.Exists(dll.FullPath.Replace(".dll", "_orig.dll"))) { crackSignals = true; crack = "_orig.dll"; }
            else if (File.Exists(dll.FullPath.Replace(".dll", "_legit.dll"))) { crackSignals = true; crack = "_legit.dll"; }
            else if (File.Exists(Path.Combine(dir, "steam_emu.ini"))) { crackSignals = true; crack = "steam_emu.ini"; }
            else if (File.Exists(Path.Combine(dir, "ColdClientLoader.ini"))) { crackSignals = true; crack = "ColdClientLoader.ini"; }
            else if (File.Exists(Path.Combine(dir, "steam_settings"))) { crackSignals = true; crack = "steam_settings"; }

            if (crackSignals)
            {
                var sub = Path.Combine(dir, ".goldberg_backup");
                Directory.CreateDirectory(sub);
                return new BackupDecision
                {
                    Method = BackupMethod.Subfolder,
                    Path = Path.Combine(sub, Path.GetFileName(dll.FullPath)),
                    DetectedCrack = crack
                };
            }

            return new BackupDecision
            {
                Method = BackupMethod.Bak,
                Path = bakPath
            };
        }

        public static void Apply(DllInfo dll)
        {
            var decision = DecideBackup(dll);
            if (File.Exists(decision.Path))
                File.Delete(decision.Path);

            File.Move(dll.FullPath, decision.Path);

            dll.BackupMethod = decision.Method == BackupMethod.Bak ? "bak" : "subfolder";
            dll.BackupPath = decision.Path;
            dll.DetectedCrack = decision.DetectedCrack;

            if (!string.IsNullOrEmpty(decision.DetectedCrack))
                dll.PreservedCrackBackups.Add(decision.DetectedCrack);
        }

        public static void Restore(DllInfo dll)
        {
            if (string.IsNullOrEmpty(dll.BackupPath) || !File.Exists(dll.BackupPath))
                return;

            if (File.Exists(dll.FullPath))
                File.Delete(dll.FullPath);

            File.Move(dll.BackupPath, dll.FullPath);

            // Limpar .goldberg_backup/ se ficou vazia
            var dir = Path.GetDirectoryName(dll.BackupPath);
            if (dir != null
                && Path.GetFileName(dir).Equals(".goldberg_backup", StringComparison.OrdinalIgnoreCase)
                && Directory.Exists(dir)
                && Directory.GetFileSystemEntries(dir).Length == 0)
            {
                Directory.Delete(dir);
            }
        }
    }
}
