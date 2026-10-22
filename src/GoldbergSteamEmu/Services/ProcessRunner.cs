using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GoldbergSteamEmu.Services
{
    /// <summary>Resultado da execução de um processo externo.</summary>
    public class ProcessResult
    {
        public int ExitCode { get; set; }
        public string StdOut { get; set; } = string.Empty;
        public string StdErr { get; set; } = string.Empty;
        public bool Success => ExitCode == 0;
    }

    public static class ProcessRunner
    {
        public static ProcessResult Run(
            string exePath,
            string args,
            string workingDir,
            IDictionary<string, string> env = null,
            int timeoutMs = 300000)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            };

            if (env != null)
            {
                foreach (var kv in env)
                    psi.EnvironmentVariables[kv.Key] = kv.Value;
            }

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            using (var p = new Process { StartInfo = psi })
            {
                p.OutputDataReceived += (s, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
                p.ErrorDataReceived += (s, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.StandardInput.Close();

                if (!p.WaitForExit(timeoutMs))
                {
                    try { p.Kill(); } catch { }
                    throw new TimeoutException($"Process '{exePath}' excedeu {timeoutMs}ms");
                }

                return new ProcessResult
                {
                    ExitCode = p.ExitCode,
                    StdOut = stdout.ToString(),
                    StdErr = stderr.ToString()
                };
            }
        }
    }
}
