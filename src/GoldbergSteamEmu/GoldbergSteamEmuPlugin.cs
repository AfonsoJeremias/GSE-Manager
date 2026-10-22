using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Playnite.SDK;
using Playnite.SDK.Plugins;

namespace GoldbergSteamEmu
{
    public class GoldbergSteamEmuPlugin : GenericPlugin
    {
        public static readonly ILogger Logger = LogManager.GetLogger();
        public static string PluginDataPath { get; private set; }
        public static string PluginInstallPath { get; private set; }

        public override Guid Id => Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

        public GoldbergSteamEmuPlugin(IPlayniteAPI api) : base(api)
        {
            Properties = new GenericPluginProperties { HasSettings = false };
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            PluginInstallPath = Path.GetDirectoryName(typeof(GoldbergSteamEmuPlugin).Assembly.Location);
            PluginDataPath = GetPluginUserDataPath();
            Directory.CreateDirectory(PluginDataPath);
            Logger.Info($"GSE Manager loaded. Install: {PluginInstallPath}, Data: {PluginDataPath}");
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var game = args.Games?.Count > 0 ? args.Games[0] : null;
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
                        Logger.Error(ex, "Failed to open GSE Manager window");
                        PlayniteApi.Dialogs.ShowErrorMessage($"Erro: {ex.Message}", "GSE Manager");
                    }
                }
            };
        }
    }
}