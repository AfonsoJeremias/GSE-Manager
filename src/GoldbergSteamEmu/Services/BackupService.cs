using System;
using System.IO;

namespace GoldbergSteamEmu.Services
{
    public static class BackupService
    {
        public static (BackupMethod method, string path) DecideBackup(DllInfo dll)
        {
            var dir = Path.GetDirectoryName(dll.FullPath);
            var nameNoExt = Path.GetFileNameWithoutExtension(dll.FullPath);
            var bakPath = dll.FullPath + ".bak";

            bool crackSignals =
                File.Exists(bakPath) ||
                File.Exists(dll.FullPath + ".org") ||
                File.Exists(Path.Combine(dir, "steam_emu.ini")) ||
                File.Exists(Path.Combine(dir, "ColdClientLoader.ini")) ||
                File.Exists(Path.Combine(dir, "steam_settings"));

            if (crackSignals)
            {
                var sub = Path.Combine(dir, ".goldberg_backup");
                Directory.CreateDirectory(sub);
                var subPath = Path.Combine(sub, Path.GetFileName(dll.FullPath));
                return (BackupMethod.Subfolder, subPath);
            }
            return (BackupMethod.Bak, bakPath);
        }

        public static void Apply(DllInfo dll)
        {
            var (method, path) = DecideBackup(dll);
            if (File.Exists(path))
                File.Delete(path);
            if (method == BackupMethod.Bak)
                File.Move(dll.FullPath, path);
            else
                File.Move(dll.FullPath, path);
            dll.BackupMethod = method == BackupMethod.Bak ? "bak" : "subfolder";
            dll.BackupPath = path;
        }

        public static void Restore(DllInfo dll)
        {
            if (!File.Exists(dll.BackupPath)) return;
            if (File.Exists(dll.FullPath)) File.Delete(dll.FullPath);
            File.Move(dll.BackupPath, dll.FullPath);
            var dir = Path.GetDirectoryName(dll.BackupPath);
            if (Path.GetFileName(dir) == ".goldberg_backup" && Directory.Exists(dir) && Directory.GetFiles(dir).Length == 0)
                Directory.Delete(dir);
        }
    }
}