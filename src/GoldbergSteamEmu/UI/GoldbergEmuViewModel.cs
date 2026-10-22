private async Task InitializeAsync()
{
    try
    {
        var installDir = _game.InstallDirectory;
        if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
        {
            StateText = $"Pasta do jogo inválida: {installDir}";
            CanApply = false;
            CanRestore = false;
            return;
        }

        var st = Services.StateDetectionService.Detect(installDir);
        StateText = st.Message;
        CanApply = st.State == EmulatorState.Original || st.State == EmulatorState.Dirty;
        CanRestore = st.State == EmulatorState.Deployed || st.State == EmulatorState.Dirty;

        foreach (var d in Services.DllDiscoveryService.Discover(installDir))
            Dlls.Add(new DllItemViewModel { Path = d.OriginalPath, Arch = d.Arch });

        if (Dlls.Count == 0)
            StateText += " (nenhuma DLL Steam encontrada)";

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
