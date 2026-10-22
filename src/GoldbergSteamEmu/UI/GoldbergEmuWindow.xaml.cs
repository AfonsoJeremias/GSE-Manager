using System.Windows;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace GoldbergSteamEmu.UI
{
    public partial class GoldbergEmuWindow : Window
    {
        public GoldbergEmuWindow(IPlayniteAPI api, Game game, string emuRoot)
        {
            InitializeComponent();
            var vm = new GoldbergEmuViewModel(api, game, emuRoot);
            DataContext = vm;
            PwdBox.PasswordChanged += (s, e) => vm.Password = PwdBox.Password;
        }
    }
}