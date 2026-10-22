using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace GoldbergSteamEmu.UI
{
    public class DllItemViewModel : INotifyPropertyChanged
    {
        public string Path { get; set; }
        public string Arch { get; set; }
        private bool _selected = true;
        public bool Selected { get => _selected; set { _selected = value; OnPropertyChanged(); } }
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class GoldbergEmuViewModel : INotifyPropertyChanged
    {
        private readonly IPlayniteAPI _api;
        private readonly Game _game;
        private readonly string _emuRoot;

        public ObservableCollection<DllItemViewModel> Dlls { get; } = new ObservableCollection<DllItemViewModel>();

        private string _stateText;
        public string StateText { get => _stateText; set { _stateText = value; OnPropertyChanged(); } }

        private string _appId;
        public string AppId { get => _appId; set { _appId = value; OnPropertyChanged(); } }

        private bool _useAnon = true;
        public bool UseAnon { get => _useAnon; set { _useAnon = value; OnPropertyChanged(); } }

        private string _username;
        public string Username { get => _username; set { _username = value; OnPropertyChanged(); } }

        private string _password;
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }

        private bool _acwEnabled;
        public bool AcwEnabled { get => _acwEnabled; set { _acwEnabled = value; OnPropertyChanged(); } }

        private bool _canApply;
        public bool CanApply { get => _canApply; set { _canApply = value; OnPropertyChanged(); } }

        private bool _canRestore;
        public bool CanRestore { get => _canRestore; set { _canRestore = value; OnPropertyChanged(); } }

        public ICommand ApplyCommand { get; }
        public ICommand RestoreCommand { get; }

        public GoldbergEmuViewModel(IPlayniteAPI api, Game game, string emuRoot)
        {
            _api = api; _game = game; _emuRoot = emuRoot;
            ApplyCommand = new RelayCommand(async _ => await ApplyAsync(), _ => CanApply);
            RestoreCommand = new RelayCommand(async _ => await RestoreAsync(), _ => CanRestore);
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            var installDir = _game.InstallDirectory;
            var st = Services.StateDetectionService.Detect(installDir);
            StateText = st.Message;
            CanApply = st.State == Services.EmulatorState.Original || st.State == Services.EmulatorState.Dirty;
            CanRestore = st.State == Services.EmulatorState.Deployed || st.State == Services.EmulatorState.Dirty;

            foreach (var d in Services.DllDiscoveryService.Discover(installDir))
                Dlls.Add(new DllItemViewModel { Path = d.OriginalPath, Arch = d.Arch });

            var appIdRes = await Services.AppIdResolver.ResolveAsync(installDir, _game.Name, _game.GameId);
            AppId = appIdRes.AppId ?? "";
        }

        private async Task ApplyAsync()
        {
            await Task.Run(() =>
            {
                var installDir = _game.InstallDirectory;
                var opts = new Services.DeployOptions
                {
                    UseAnonymousAccount = UseAnon,
                    Username = Username,
                    Password = Password,
                    DeployAchievementWatcher = AcwEnabled
                };
                var outputDir = Path.Combine(installDir, "_OUTPUT", AppId);
                var res = Services.PipelineOrchestrator.Apply(installDir, AppId, _emuRoot, outputDir, opts, s => { });
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (res.Success) { _api.Dialogs.ShowMessage("Aplicado com sucesso.", "GSE Manager"); }
                    else { _api.Dialogs.ShowErrorMessage(res.Error, "GSE Manager"); }
                });
            });
        }

        private async Task RestoreAsync()
        {
            await Task.Run(() =>
            {
                var res = Services.PipelineOrchestrator.Restore(_game.InstallDirectory, s => { });
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (res.Success) { _api.Dialogs.ShowMessage("Restaurado.", "GSE Manager"); }
                    else { _api.Dialogs.ShowErrorMessage(res.Error, "GSE Manager"); }
                });
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}