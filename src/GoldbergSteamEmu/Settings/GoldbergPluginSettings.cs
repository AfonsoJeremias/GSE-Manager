using System;
using System.IO;
using Newtonsoft.Json;

namespace GoldbergSteamEmu.Settings
{
    public class GoldbergPluginSettings
    {
        public bool UseAnon { get; set; } = true;
        public bool DeployAchievementWatcher { get; set; } = false;
        public string EmuVersion { get; set; } = "release";

        [JsonIgnore]
        public string Username
        {
            get { return PasswordProtector.Unprotect(UsernameEnc); }
            set { UsernameEnc = PasswordProtector.Protect(value); }
        }

        [JsonIgnore]
        public string Password
        {
            get { return PasswordProtector.Unprotect(PasswordEnc); }
            set { PasswordEnc = PasswordProtector.Protect(value); }
        }

        public string UsernameEnc { get; set; } = "";
        public string PasswordEnc { get; set; } = "";

        public static GoldbergPluginSettings Load(string path)
        {
            if (!File.Exists(path)) return new GoldbergPluginSettings();
            try { return JsonConvert.DeserializeObject<GoldbergPluginSettings>(File.ReadAllText(path)) ?? new GoldbergPluginSettings(); }
            catch { return new GoldbergPluginSettings(); }
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }

    public static class PasswordProtector
    {
        public static string Protect(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(plain);
                var enc = System.Security.Cryptography.ProtectedData.Protect(
                    bytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(enc);
            }
            catch { return ""; }
        }

        public static string Unprotect(string b64)
        {
            if (string.IsNullOrEmpty(b64)) return "";
            try
            {
                var bytes = Convert.FromBase64String(b64);
                var dec = System.Security.Cryptography.ProtectedData.Unprotect(
                    bytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return System.Text.Encoding.UTF8.GetString(dec);
            }
            catch { return ""; }
        }
    }
}
