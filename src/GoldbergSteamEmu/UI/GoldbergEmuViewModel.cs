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
        public bool Selected
        {
            get { return _selected; }
            set { _selected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class GoldbergEmuViewModel : INotifyPropertyChanged
    {
        private readonly IPlayniteAPI _api;
        private readonly Game _game;
        private readonly string _emuRoot;

        public ObservableCollection<DllItemViewModel> Dlls { get; } = new ObservableCollection<DllItemViewModel>();

        private string _stateText;
        public string StateText
        {
            get { return _stateText; }
            set { _stateText = value; OnPropertyChanged(); }
        }

        private string _appId;
        public string AppId
        {
            get { return _appId; }
            set { _appId = value; OnPropertyChanged(); }
        }

        private bool _useAnon = true;
        public bool UseAnon
        {
            get { return _useAnon; }
            set { _useAnon = value; OnPropertyChanged(); }
        }

        private string _username;
        public string Username
        {
            get { return _username; }
            set { _username = value; OnPropertyChanged(); }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set { _password = value; OnPropertyChanged(); }
        }

        private bool _acwEnabled;
        public bool AcwEnabled
        {
            get { return _acwEnabled; }
            set { _acwEnabled = value; OnPropertyChanged(); }
        }

        private bool _canApply;
        public bool CanApply
        {
            get { return _canApply; }
            set { _canApply = value; OnPropertyChanged(); }
        }

        private bool _canRestore;
        public bool CanRestore
        {
            get { return _canRestore; }
            set { _canRestore = value; OnPropertyChanged(); }
        }

        public ICommand ApplyCommand { get; }
        public ICommand RestoreCommand { get; }

        public GoldbergEmuViewModel(IPlayniteAPI api, Game game, string emuRoot)
        {
            _api = api;
            _game = game;
            _emuRoot = emuRoot;

            ApplyCommand = new RelayCommand(async _ => await ApplyAsync(), _ => CanApply);
            RestoreCommand = new RelayCommand(async _ => await RestoreAsync(), _ => CanRestore);

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var installDir = _game.InstallDirectory;
                if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
                {
                    StateText = string.Format("Pasta do jogo inválida: {0}", installDir);
                    CanApply = false;
                    CanRestore = false;
                    return;
                }

                var st = Services.StateDetectionService.Detect(installDir);
                StateText = st.Message;
                CanApply = st.State == EmulatorState.Original || st.State == EmulatorState.Dirty;
                CanRestore = st.State == EmulatorState.Deployed || st.State == EmulatorState.Dirty;

                foreach (var d in Services.DllDiscoveryService.Discover(installDir))
                {
                    Dlls.Add(new DllItemViewModel { Path = d.OriginalPath, Arch = d.Arch });
                }

                if (Dlls.Count == 0)
                {
                    StateText += " (nenhuma DLL Steam encontrada)";
                }

                var appIdRes = await Services.AppIdResolver.ResolveAsync(installDir, _game.Name, _game.GameId);
                AppId = appIdRes.AppId ?? string.Empty;
            }
            catch (Exception ex)
            {
                StateText = "Erro: " + ex.Message;
                CanApply = false;
                CanRestore = false;
                GoldbergSteamEmuPlugin.Logger.Error(ex, "InitializeAsync falhou");
            }
        }

        private async Task ApplyAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    var installDir = _game.InstallDirectory;
                    var opts = new DeployOptions
                    {
                        UseAnonymousAccount = UseAnon,
                        Username = Username,
                        Password = Password,
                        DeployAchievementWatcher = AcwEnabled
                    };

                    var outputDir = Path.Combine(installDir, "_OUTPUT", AppId);

                    var res = Services.PipelineOrchestrator.Apply(
                        installDir, AppId, _emuRoot, outputDir, opts, s => { });

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (res.Success)
                            _api.Dialogs.ShowMessage("Goldberg aplicado com sucesso.", "GSE Manager");
                        else
                            _api.Dialogs.ShowErrorMessage(res.Error ?? "Erro desconhecido.", "GSE Manager");
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        _api.Dialogs.ShowErrorMessage(ex.Message, "GSE Manager"));
                }
            });
        }

        private async Task RestoreAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    var res = Services.PipelineOrchestrator.Restore(_game.InstallDirectory, s => { });

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (res.Success)
                            _api.Dialogs.ShowMessage("Ficheiros originais restaurados.", "GSE Manager");
                        else
                            _api.Dialogs.ShowErrorMessage(res.Error ?? "Erro desconhecido.", "GSE Manager");
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        _api.Dialogs.ShowErrorMessage(ex.Message, "GSE Manager"));
                }
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
