using System;
using System.Collections.Generic;
using System.IO;

namespace GoldbergSteamEmu.Services
{
    public static class InterfaceGenerationService
    {
        public static void GenerateForDll(string emuRoot, DllInfo dll, string dllDir)
        {
            if (dll == null || string.IsNullOrEmpty(dll.BackupPath) || !File.Exists(dll.BackupPath))
            {
                GoldbergSteamEmuPlugin.Logger.Warn(
                    $"InterfaceGeneration: backup não encontrado para {dll?.OriginalPath ?? "(null)"}");
                return;
            }

            var toolDir = Path.Combine(emuRoot, "tools", "generate_interfaces");
            if (!Directory.Exists(toolDir))
            {
                GoldbergSteamEmuPlugin.Logger.Warn($"Pasta de tools não encontrada: {toolDir}");
                return;
            }

            var exePath = ResolveInterfaceExe(toolDir, dll.Arch);
            if (exePath == null)
            {
                GoldbergSteamEmuPlugin.Logger.Warn(
                    $"Nenhum generate_interfaces*.exe encontrado em {toolDir} (arch={dll.Arch})");
                return;
            }

            var args = "\"" + dll.BackupPath + "\"";
            var result = ProcessRunner.Run(exePath, args, dllDir, timeoutMs: 60000);

            if (!result.Success)
            {
                GoldbergSteamEmuPlugin.Logger.Warn(
                    $"generate_interfaces exit={result.ExitCode} para {dll.OriginalPath}\n" +
                    $"STDOUT:\n{result.StdOut}\nSTDERR:\n{result.StdErr}");
            }

            FinalizeInterfaces(dllDir);
        }

        private static string ResolveInterfaceExe(string toolDir, string arch)
        {
            var candidates = new List<string>();
            if (string.Equals(arch, "x64", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add("generate_interfaces64.exe");
                candidates.Add("generate_interfaces_x64.exe");
                candidates.Add("generate_interfaces.exe");
            }
            else
            {
                candidates.Add("generate_interfaces.exe");
                candidates.Add("generate_interfaces_x32.exe");
                candidates.Add("generate_interfaces64.exe");
            }

            foreach (var name in candidates)
            {
                var full = Path.Combine(toolDir, name);
                if (File.Exists(full)) return full;
            }
            return null;
        }

        private static void FinalizeInterfaces(string dllDir)
        {
            var txtPath = Path.Combine(dllDir, "steam_interfaces.txt");
            if (!File.Exists(txtPath))
            {
                GoldbergSteamEmuPlugin.Logger.Warn($"steam_interfaces.txt não foi gerado em {dllDir}");
                return;
            }

            var ssDir = Path.Combine(dllDir, "steam_settings");
            Directory.CreateDirectory(ssDir);

            var txtDest = Path.Combine(ssDir, "steam_interfaces.txt");
            if (File.Exists(txtDest)) File.Delete(txtDest);
            File.Move(txtPath, txtDest);

            var content = File.ReadAllText(txtDest);
            var iniDest = Path.Combine(ssDir, "steam_interfaces.ini");
            File.WriteAllText(iniDest, "[steam_interfaces]" + Environment.NewLine + content);

            var steamEmuIni = Path.Combine(dllDir, "steam_emu.ini");
            if (File.Exists(steamEmuIni))
            {
                try { InjectIntoSteamEmuIni(steamEmuIni, content); }
                catch (Exception ex)
                {
                    GoldbergSteamEmuPlugin.Logger.Warn(
                        $"Falha ao injetar interfaces em {steamEmuIni}: {ex.Message}");
                }
            }
        }

        private static void InjectIntoSteamEmuIni(string steamEmuIniPath, string interfacesTxt)
        {
            var lines = new List<string>(File.ReadAllLines(steamEmuIniPath));

            var interfaces = new List<string>();
            foreach (var raw in interfacesTxt.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var t = raw.Trim();
                if (t.Length == 0) continue;
                interfaces.Add(t.Contains("=") ? t : (t + "=" + t));
            }

            int sectionStart = -1, sectionEnd = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    if (sectionStart >= 0 && sectionEnd < 0) sectionEnd = i;
                    if (line.Equals("[Interfaces]", StringComparison.OrdinalIgnoreCase)) sectionStart = i;
                }
            }
            if (sectionStart >= 0 && sectionEnd < 0) sectionEnd = lines.Count;

            if (sectionStart < 0)
            {
                if (lines.Count > 0 && lines[lines.Count - 1].Trim().Length > 0) lines.Add(string.Empty);
                lines.Add("[Interfaces]");
                lines.AddRange(interfaces);
            }
            else
            {
                var newLines = new List<string>();
                newLines.AddRange(lines.GetRange(0, sectionStart + 1));
                newLines.AddRange(interfaces);
                if (sectionEnd < lines.Count)
                    newLines.AddRange(lines.GetRange(sectionEnd, lines.Count - sectionEnd));
                lines = newLines;
            }

            File.WriteAllLines(steamEmuIniPath, lines);
        }
    }
}
