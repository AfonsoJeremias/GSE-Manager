using System;
using System.Diagnostics;
using System.Text;

namespace GoldbergSteamEmu.Services
{
    public static class ProcessRunner
    {
        public static (int exitCode, string stdout, string stderr) Run(
            string exePath, string args, string workingDir, System.Collections.Generic.IDictionary<string, string> env = null, int timeoutMs = 300000)
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
                foreach (var kv in env) psi.EnvironmentVariables[kv.Key] = kv.Value;

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
                return (p.ExitCode, stdout.ToString(), stderr.ToString());
            }
        }
    }
}