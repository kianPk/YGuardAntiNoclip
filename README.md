# YGuard Anti Noclip

Blocks `noclip` / `bind x noclip` / `css_noclip` on **Public, Custom, Ranked**.

Practice servers are left alone (utility training needs noclip).

By default **nobody** can noclip â€” including panel owners â€” so it is easy to verify. Set `AllowAdminNoclip: true` in config if root admins should keep it.

## Panel install (Plugin Directory)

Release zip root is the plugin folder (`YGuardAntiNoclip.dll`) â€” same layout as YGuardVIP.

1. **RELEASE URL**
   ```text
   https://github.com/kianPk/YGuardAntiNoclip/releases/download/v1.0.5/YGuardAntiNoclip-1.0.5.zip
   ```
2. **FRAMEWORK:** CounterStrikeSharp
3. **ARCHIVE LAYOUT:** Archive root is the plugin folder
4. **INSTALL PATH:** `addons/counterstrikesharp/plugins/YGuardAntiNoclip`
5. Add to catalog â†’ Install on nodes until rollout is green
6. **Required:** turn **Ranked Matches** ON (without this the DLL is installed but never linked into Public servers)
7. Restart the Public dedicated server

Servers-section servers (Duels / DM / BHOP / 2x2) load it on their own once it
is installed on the node: they skip the Ranked Matches toggle and list it as
one of their mode's plugins. Recreate those servers after installing.

Zip root is flat (`YGuardAntiNoclip.dll`), with forward-slash paths so Linux `unzip` works.

### Prove it loaded (RCON)

```text
css_antinoclip_status
```

You should see `active=true`. On map start chat also shows `[AntiNoclip] active`.

## Config

`configs/plugins/YGuardAntiNoclip/YGuardAntiNoclip.json`

```json
{
  "ChatPrefix": "AntiNoclip",
  "AllowAdminNoclip": false,
  "AdminFlag": "@css/root",
  "SkipPractice": true,
  "AllowPluginNoclip": false
}
```

`AllowPluginNoclip: true` keeps sv_cheats locked and cheat commands blocked
but leaves noclip that another plugin grants (ServersBhop's `!noclip`
practice) alone. The api writes it for BHOP servers.

## Build

```bash
dotnet build -c Release
```
