using System.IO;
using System.Windows.Controls;
using Playnite.SDK;
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
            TxtUsername.Text = s.Username;
            TxtPassword.Password = s.Password;
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
            s.Username = TxtUsername.Text;
            s.Password = TxtPassword.Password;
            s.DeployAchievementWatcher = ChkAcw.IsChecked == true;
            s.EmuVersion = (CmbVersion.SelectedIndex == 1) ? "debug" : "release";

            _plugin.Settings = s;
            _plugin.SaveSettings();
        }

        public UserControl GetView(GetSettingsViewArgs args) { return this; }

        public void Dispose() { }
    }
}
