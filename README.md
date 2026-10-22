# GSE Manager — Goldberg Steam Emulator para Playnite

Extensão para o [Playnite](https://playnite.link/) que integra o [Goldberg Steam Emulator (GSE)](https://github.com/Mr-Goldberg/steam-emu) diretamente na biblioteca de jogos, com interface gráfica, deteção automática de DLLs, toggle de estados com rollback fiável e edição de configurações por jogo.

Substitui o clássico pacote "semi-auto V13" (`.bat` + AutoIt + AdvancedRun) por uma experiência nativa, sem janelas CMD, sem scripts externos e sem escrever credenciais em texto simples.

> ⚠️ **Aviso legal:** esta ferramenta faz bypass de DRM/Steamworks e é destinada a jogos que **não** possuis na Steam. Usa por tua conta e risco. Não apoia pirataria de software que podes comprar legalmente. O autor não se responsabiliza por uso indevido.

---

## ✨ Funcionalidades

- **Menu de contexto no Playnite** — aparece apenas em jogos **instalados** da biblioteca.
- **Deteção automática** de `steam_api.dll`, `steam_api64.dll`, `steamclient.dll`, `steamclient64.dll` em toda a pasta do jogo (recursivo), com arquitetura determinada pelo header PE.
- **Aplica a todas as DLLs encontradas** (não só uma), com `steam_settings/` por localização.
- **Modelo de 2 estados estritos**:
  - **Estado 0 — Original**: DLLs originais no lugar, sem vestígios de Goldberg.
  - **Estado 1 — Emulador**: DLLs do Goldberg no lugar, originais guardadas em backup.
  - Estados sujos (jogo atualizado, ficheiros apagados) são detetados e reparáveis.
- **Backups híbridos**:
  - `.bak` ao lado da DLL (caso normal).
  - `.goldberg_backup/` por localização se já existir crack pré-existente (CODEX/RUNE/EMPRESS), para não destruir o `.bak` do crack.
- **Manifest JSON** (`.goldberg_manifest.json`) com hashes, data e caminhos, para restore fiável.
- **Resolução de AppID em cascata**: `Game.GameId` (Steam), `steam_appid.txt`, `steam_settings/steam_appid.txt`, `steam_emu.ini`, `ColdClientLoader.ini`, pesquisa por nome, e override manual.
- **Conta anónima por omissão**; credenciais reais opcionais, guardadas com **DPAPI** (mesmo mecanismo do Playnite) e passadas via variáveis de ambiente `GSE_CFG_USERNAME` / `GSE_CFG_PASSWORD` (nunca em texto simples).
- **Geração de interfaces** por DLL (`steam_interfaces.txt` + `.ini`) e injeção automática em `steam_emu.ini` de cracks CODEX/RUNE.
- **Achievement Watcher opcional** — extrai `extra_acw.zip` e regista a pasta de saves no `userdir.db`, com backup.
- **Configurações globais e por jogo** dos 4 INIs (`configs.app.ini`, `configs.main.ini`, `configs.overlay.ini`, `configs.user.ini`), preservando comentários.
- **Execução totalmente em segundo plano** — `generate_emu_config`, `generate_interfaces` e `7za` correm sem janelas CMD, com stdout/stderr capturados.

---

## 📋 Requisitos

- **Playnite** 10.63 ou superior.
- **Playnite SDK** 6.18.0 (referenciado via NuGet).
- **.NET Framework 4.6.2** (runtime exigido pelos plugins do Playnite).
- Windows 10/11.

---

## 📦 Instalação

1. Descarrega o `.pext` mais recente da aba [Releases](../../releases).
2. No Playnite: **Add-ons** → **Install from file** → seleciona o `.pext`.
3. Reinicia o Playnite.
4. A extensão aparece no **menu de contexto** de qualquer jogo instalado, na secção **GSE Manager**.

---

## 🚀 Utilização

1. Clica com o botão direito num jogo **instalado** da biblioteca → **GSE Manager** → **Goldberg Steam Emu**.
2. A janela abre com:
   - **Estado detetado** (0, 1 ou sujo).
   - **Lista de DLLs encontradas** com arquitetura.
   - **AppID** (auto-detetado ou editável).
   - **Conta** (anónima por omissão).
3. Ajusta as **configurações** (globais ou por jogo) nas abas correspondentes.
4. Clica em **Aplicar Goldberg** (se estado 0) ou **Restaurar Original** (se estado 1).

### Modelo de estados

| Estado | O que significa | Ação permitida |
|---|---|---|
| **0 — Original** | Sem vestígios de Goldberg | Só **Aplicar** |
| **1 — Emulador** | Goldberg aplicado | Só **Restaurar** |
| **Sujo** | Atualização do jogo, ficheiros apagados, ambiguidade | **Reparar para 0** ou **Reparar para 1** |

Para mudar configurações num jogo já aplicado, é necessário **Restaurar → Aplicar novamente**.

---

## ⚙️ Configurações

### Globais (menu de extensão)
- Conta (anónima por omissão ou credenciais DPAPI).
- Defaults dos 4 INIs.
- Opções de conteúdo (conquistas, imagens, interfaces, ACW).
- Versão do emulador (release/debug).

### Por jogo (menu de contexto)
- Override do AppID.
- Overrides dos 4 INIs por campo tipado (`language`, `account_steamid`, `ip_country`, `unlock_all`, etc.).
- Modo raw para edição livre do INI.
- Exclusão de DLLs específicas.

**Nota:** todos os overrides são aplicados a **todas as cópias** de `steam_settings/` (uma por localização de DLL) antes de distribuir.

---

## 🛠️ Compilar a partir do código

```bash
git clone https://github.com/AfonsoJeremias/GSE-Manager
cd GoldbergSteamEmu

# Fazer build (Windows, VS Build Tools / MSBuild):
msbuild src/GoldbergSteamEmu/GoldbergSteamEmu.csproj /p:Configuration=Release

# Empacotar com o Toolbox do Playnite:
Toolbox.exe pack src/GoldbergSteamEmu/bin/Release dist
