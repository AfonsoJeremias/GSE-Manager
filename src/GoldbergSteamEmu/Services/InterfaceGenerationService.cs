using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GoldbergSteamEmu.Services
{
    /// <summary>
    /// Gera o ficheiro steam_interfaces.txt/.ini para cada DLL substituída,
    /// chamando o generate_interfaces*.exe correspondente à arquitetura da DLL.
    /// Também injeta as interfaces em steam_emu.ini de cracks CODEX/RUNE, se existirem.
    /// </summary>
    public static class InterfaceGenerationService
    {
        /// <summary>
        /// Gera as interfaces para uma DLL já substituída.
        /// Corre o executável em segundo plano (sem janela), sobre o ficheiro .bak/.goldberg_backup.
        /// </summary>
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

            // O generate_interfaces escreve steam_interfaces.txt no working directory.
            // Usamos a pasta da DLL para que o ficheiro fique ao lado da steam_settings/.
            var args = "\"" + dll.BackupPath + "\"";
            var (exitCode, stdout, stderr) = ProcessRunner.Run(
                exePath, args, dllDir, timeoutMs: 60000);

            if (exitCode != 0)
            {
                GoldbergSteamEmuPlugin.Logger.Warn(
                    $"generate_interfaces exit={exitCode} para {dll.OriginalPath}\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
            }

            FinalizeInterfaces(dllDir);
        }

        /// <summary>
        /// Escolhe o executável correto conforme a arquitetura, com fallbacks
        /// para nomes alternativos (generate_interfaces_x64.exe, generate_interfaces64.exe).
        /// </summary>
        private static string ResolveInterfaceExe(string toolDir, string arch)
        {
            // Lista de candidatos, por ordem de preferência
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
                candidates.Add("generate_interfaces64.exe"); // último recurso
            }

            foreach (var name in candidates)
            {
                var full = Path.Combine(toolDir, name);
                if (File.Exists(full))
                    return full;
            }
            return null;
        }

        /// <summary>
        /// Move steam_interfaces.txt para steam_settings/, cria steam_interfaces.ini
        /// e injeta em steam_emu.ini se existir (CODEX/RUNE).
        /// </summary>
        private static void FinalizeInterfaces(string dllDir)
        {
            var txtPath = Path.Combine(dllDir, "steam_interfaces.txt");
            if (!File.Exists(txtPath))
            {
                GoldbergSteamEmuPlugin.Logger.Warn(
                    $"steam_interfaces.txt não foi gerado em {dllDir}");
                return;
            }

            var ssDir = Path.Combine(dllDir, "steam_settings");
            Directory.CreateDirectory(ssDir);

            // steam_interfaces.txt → steam_settings/
            var txtDest = Path.Combine(ssDir, "steam_interfaces.txt");
            if (File.Exists(txtDest)) File.Delete(txtDest);
            File.Move(txtPath, txtDest);

            // steam_interfaces.ini (com cabeçalho [steam_interfaces])
            var content = File.ReadAllText(txtDest);
            var iniDest = Path.Combine(ssDir, "steam_interfaces.ini");
            File.WriteAllText(iniDest, "[steam_interfaces]" + Environment.NewLine + content);

            // Injetar em steam_emu.ini (CODEX/RUNE) se existir
            var steamEmuIni = Path.Combine(dllDir, "steam_emu.ini");
            if (File.Exists(steamEmuIni))
            {
                try
                {
                    InjectIntoSteamEmuIni(steamEmuIni, content);
                }
                catch (Exception ex)
                {
                    GoldbergSteamEmuPlugin.Logger.Warn(
                        $"Falha ao injetar interfaces em {steamEmuIni}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Adiciona as interfaces à secção [Interfaces] do steam_emu.ini de CODEX/RUNE.
        /// Se a secção não existir, cria-a no fim do ficheiro.
        /// </summary>
        private static void InjectIntoSteamEmuIni(string steamEmuIniPath, string interfacesTxt)
        {
            var lines = new List<string>(File.ReadAllLines(steamEmuIniPath));

            // Parse das interfaces (linha a linha, ignora vazias)
            var interfaces = new List<string>();
            foreach (var raw in interfacesTxt.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var t = raw.Trim();
                if (t.Length == 0) continue;
                // Se já tem '=', mantém; senão escreve "Name=Name"
                interfaces.Add(t.Contains("=") ? t : (t + "=" + t));
            }

            // Procurar secção [Interfaces]
            int sectionStart = -1;
            int sectionEnd = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    if (sectionStart >= 0 && sectionEnd < 0)
                        sectionEnd = i; // fim da secção [Interfaces]
                    if (line.Equals("[Interfaces]", StringComparison.OrdinalIgnoreCase))
                        sectionStart = i;
                }
            }
            if (sectionStart >= 0 && sectionEnd < 0)
                sectionEnd = lines.Count;

            if (sectionStart < 0)
            {
                // Secção não existe: adicionar no fim
                if (lines.Count > 0 && lines[lines.Count - 1].Trim().Length > 0)
                    lines.Add(string.Empty);
                lines.Add("[Interfaces]");
                lines.AddRange(interfaces);
            }
            else
            {
                // Secção existe: substituir o bloco
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