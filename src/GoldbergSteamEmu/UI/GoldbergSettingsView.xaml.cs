using System.Collections.Generic;
using System.Windows.Controls;
using Playnite.SDK;                    // <-- necessário para ISettings
using GoldbergSteamEmu.Settings;

namespace GoldbergSteamEmu.UI
{
    public partial class GoldbergSettingsView : UserControl, ISettings
    {
        private readonly GoldbergSteamEmuPlugin _plugin;

        public GoldbergSettingsView(GoldbergSteamEmuPlugin plugin)
        {
            InitializeComponent();
            _plugin = plugin;
            LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            var s = _plugin.Settings ?? new GoldbergPluginSettings();
            ChkAnon.IsChecked = s.UseAnon;
            TxtUsername.Text = s.Username ?? string.Empty;
            TxtPassword.Password = s.Password ?? string.Empty;
            ChkAcw.IsChecked = s.DeployAchievementWatcher;
            CmbVersion.SelectedIndex = s.EmuVersion == "debug" ? 1 : 0;
        }

        public void BeginEdit() { }

        public void CancelEdit()
        {
            LoadFromSettings();
        }

        public void EndEdit()
        {
            var s = _plugin.Settings ?? new GoldbergPluginSettings();
            s.UseAnon = ChkAnon.IsChecked == true;
            s.Username = TxtUsername.Text ?? string.Empty;
            s.Password = TxtPassword.Password ?? string.Empty;
            s.DeployAchievementWatcher = ChkAcw.IsChecked == true;
            s.EmuVersion = (CmbVersion.SelectedIndex == 1) ? "debug" : "release";

            _plugin.Settings = s;
            _plugin.SaveSettings();
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();

            if (ChkAnon.IsChecked == false)
            {
                if (string.IsNullOrWhiteSpace(TxtUsername.Text))
                    errors.Add("O username não pode estar vazio quando a conta anónima está desativada.");
                if (string.IsNullOrWhiteSpace(TxtPassword.Password))
                    errors.Add("A password não pode estar vazia quando a conta anónima está desativada.");
            }

            return errors.Count == 0;
        }
    }
}
