using System;
using System.Collections.Generic;
using System.IO;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using GoldbergSteamEmu.Settings;

namespace GoldbergSteamEmu
{
    public class GoldbergSteamEmuPlugin : GenericPlugin
    {
        public static readonly ILogger Logger = LogManager.GetLogger();
        public static string PluginDataPath { get; private set; }
        public static string PluginInstallPath { get; private set; }

        public GoldbergPluginSettings Settings { get; set; }

        public override Guid Id => Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

        public GoldbergSteamEmuPlugin(IPlayniteAPI api) : base(api)
        {
            Properties = new GenericPluginProperties { HasSettings = true };
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            PluginInstallPath = Path.GetDirectoryName(typeof(GoldbergSteamEmuPlugin).Assembly.Location);
            PluginDataPath = GetPluginUserDataPath();
            Directory.CreateDirectory(PluginDataPath);

            Settings = GoldbergPluginSettings.Load(Path.Combine(PluginDataPath, "settings.json"));
            Logger.Info($"GSE Manager carregado. Install: {PluginInstallPath}, Data: {PluginDataPath}");
        }

        public void SaveSettings()
        {
            Settings?.Save(Path.Combine(PluginDataPath, "settings.json"));
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return new UI.GoldbergSettingsView(this);
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            if (args.Games == null || args.Games.Count == 0) yield break;
            var game = args.Games[0];
            if (game == null || !game.IsInstalled) yield break;

            yield return new GameMenuItem
            {
                Description = "Goldberg Steam Emu",
                MenuSection = "GSE Manager",
                Action = _ =>
                {
                    try
                    {
                        var win = new UI.GoldbergEmuWindow(PlayniteApi, game, PluginInstallPath);
                        win.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Falha ao abrir a janela GSE Manager");
                        PlayniteApi.Dialogs.ShowErrorMessage($"Erro: {ex.Message}", "GSE Manager");
                    }
                }
            };
        }
    }
}
