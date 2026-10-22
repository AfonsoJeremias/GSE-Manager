using System;
using System.Collections.Generic;
using System.IO;
using Playnite.SDK;

namespace GoldbergSteamEmu.Services
{
    public static class PipelineOrchestrator
    {
        public static PipelineResult Apply(string installDir, string appId, string emuRoot, string outputAppIdDir,
            DeployOptions opts, Action<string> log)
        {
            var result = new PipelineResult { AppId = appId };
            var manifest = new GoldbergManifest { AppId = appId, State = 1, AppliedAt = DateTime.UtcNow.ToString("o"), EmuVersion = opts.EmuVersion };
            var dlls = DllDiscoveryService.Discover(installDir);
            if (dlls.Count == 0) { result.Error = "Nenhuma DLL Steam encontrada."; return result; }

            try
            {
                // 1. Backup
                foreach (var d in dlls) { log?.Invoke("Backup: " + d.OriginalPath); BackupService.Apply(d); manifest.Dlls.Add(d); }

                // 2. Canonical steam_settings
                log?.Invoke("Gerando steam_settings...");
                var canon = EmulatorDeployService.BuildCanonicalSteamSettings(emuRoot, outputAppIdDir, installDir, opts);
                manifest.CreatedPaths.Add("steam_settings");

                // 3. Deploy DLLs + steam_settings por localização
                foreach (var d in dlls)
                {
                    var dir = Path.GetDirectoryName(d.FullPath);
                    EmulatorDeployService.DeployDllsForLocation(installDir, dir, emuRoot);
                    EmulatorDeployService.CopySteamSettings(canon, dir);
                }

                // 4. Interfaces
                if (opts.GenerateInterfaces)
                {
                    log?.Invoke("Gerando interfaces...");
                    foreach (var d in dlls)
                        InterfaceGenerationService.GenerateForDll(emuRoot, d, Path.GetDirectoryName(d.FullPath));
                }

                // 5. ACW (opcional)
                if (opts.DeployAchievementWatcher)
                {
                    log?.Invoke("Implantando Achievement Watcher...");
                    var saves = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GSE Saves");
                    AchievementWatcherService.Deploy(emuRoot, saves, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
                }

                manifest.Save(Path.Combine(installDir, StateDetectionService.ManifestName));
                Directory.Delete(canon, true);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                // Rollback
                try
                {
                    foreach (var d in dlls) BackupService.Restore(d);
                    var mf = Path.Combine(installDir, StateDetectionService.ManifestName);
                    if (File.Exists(mf)) File.Delete(mf);
                }
                catch { }
            }
            return result;
        }

        public static PipelineResult Restore(string installDir, Action<string> log)
        {
            var result = new PipelineResult();
            var mfPath = Path.Combine(installDir, StateDetectionService.ManifestName);
            var manifest = GoldbergManifest.Load(mfPath);
            if (manifest == null) { result.Error = "Manifest não encontrado."; return result; }

            try
            {
                foreach (var d in manifest.Dlls)
                {
                    log?.Invoke("Restaurando: " + d.OriginalPath);
                    BackupService.Restore(d);
                }

                foreach (var p in manifest.CreatedPaths)
                {
                    var full = Path.Combine(installDir, p);
                    if (Directory.Exists(full)) Directory.Delete(full, true);
                    else if (File.Exists(full)) File.Delete(full);
                }

                if (File.Exists(mfPath)) File.Delete(mfPath);
                result.Success = true;
            }
            catch (Exception ex) { result.Error = ex.Message; }
            return result;
        }
    }
}