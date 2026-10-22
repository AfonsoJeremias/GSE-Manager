using System;
using System.Collections.Generic;
using System.IO;

namespace GoldbergSteamEmu.Services
{
    public class StateDetectionResult
    {
        public EmulatorState State { get; set; }
        public GoldbergManifest Manifest { get; set; }
        public bool ManifestMissing { get { return Manifest == null; } }
        public string Message { get; set; }
    }

    public static class StateDetectionService
    {
        public const string ManifestName = ".goldberg_manifest.json";

        public static StateDetectionResult Detect(string installDir)
        {
            var res = new StateDetectionResult();
            var manifestPath = Path.Combine(installDir, ManifestName);
            res.Manifest = GoldbergManifest.Load(manifestPath);

            var dlls = DllDiscoveryService.Discover(installDir);
            int bakCount = 0, subBackupCount = 0, liveDlls = 0;

            foreach (var d in dlls)
            {
                liveDlls++;
                if (File.Exists(d.FullPath + ".bak")) bakCount++;
                var sub = Path.Combine(Path.GetDirectoryName(d.FullPath), ".goldberg_backup");
                if (Directory.Exists(sub)) subBackupCount++;
            }

            if (res.Manifest != null && res.Manifest.State == 1)
            {
                res.State = EmulatorState.Deployed;
                res.Message = "Manifest do emulador encontrado.";
                return res;
            }

            if (liveDlls == 0)
            {
                res.State = EmulatorState.Unknown;
                res.Message = "Nenhuma DLL Steam encontrada na pasta do jogo.";
                return res;
            }

            var ssVersionFile = Path.Combine(installDir, "steam_settings", "emu_version.txt");
            if (bakCount == 0 && subBackupCount == 0 && !File.Exists(ssVersionFile))
            {
                res.State = EmulatorState.Original;
                res.Message = "Nenhum vestígio de Goldberg detetado.";
                return res;
            }

            res.State = EmulatorState.Dirty;
            res.Message = "Vestígios encontrados sem manifest — reparar necessário.";
            return res;
        }
    }
}
