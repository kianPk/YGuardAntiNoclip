# YGuard Anti Noclip

Blocks `noclip` / `bind x noclip` / `css_noclip` on **Public, Custom, Ranked**.

Practice servers are left alone (utility training needs noclip).

By default **nobody** can noclip — including panel owners — so it is easy to verify. Set `AllowAdminNoclip: true` in config if root admins should keep it.

## Panel install (Plugin Directory)

Release zip uses the `game/csgo` layout (`addons/...`).

1. **RELEASE URL**
   ```text
   https://github.com/kianPk/YGuardAntiNoclip/releases/download/v1.0.0/YGuardAntiNoclip-1.0.0.zip
   ```
2. **FRAMEWORK:** CounterStrikeSharp
3. **ARCHIVE LAYOUT:** Archive root is game/csgo
4. Add to catalog → Install on nodes → enable load for Public / Custom

## Config

`configs/plugins/YGuardAntiNoclip/YGuardAntiNoclip.json`

```json
{
  "ChatPrefix": "AntiNoclip",
  "AllowAdminNoclip": false,
  "AdminFlag": "@css/root",
  "SkipPractice": true
}
```

## Build

```bash
dotnet build -c Release
```
